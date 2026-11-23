using CredentialAuthority.App.Core.Models;
using CredentialAuthority.App.Features.CreateRequest;
using CredentialAuthority.App.Features.GetCredential;
using CredentialAuthority.App.Features.IssueCredential;
using CredentialAuthority.App.SharedPorts;
using CredentialAuthority.Infra.Features.CreateRequest;
using CredentialAuthority.Infra.Features.GetCredential;
using CredentialAuthority.Infra.Features.IssueCredential;
using CredentialAuthority.Infra.Integrations.Sqlite;
using CredentialAuthority.Infra.SharedAdapters;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace CredentialAuthority.Infra.Tests;

public sealed class SqliteWorkflowTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "code-review-task-tests", Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private SqliteDatabase _database = null!;
    private ServiceProvider _services = null!;

    public async Task InitializeAsync()
    {
        _database = new SqliteDatabase(Path.Combine(_directory, "test.db"));
        await _database.Initialize();
        _services = CreateServices(_database);
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        Directory.Delete(_directory, recursive: true);
    }

    private ServiceProvider CreateServices(SqliteDatabase database)
    {
        var services = new ServiceCollection();
        services.AddSingleton(database);
        services.AddSingleton<TimeProvider>(_time);
        services.AddSingleton<ILogger<ProcessPendingRequestsUseCase>>(NullLogger<ProcessPendingRequestsUseCase>.Instance);
        services.AddOptions<IssuerOptions>();
        services.AddCreateRequestFeatureApp();
        services.AddCreateRequestFeatureInfra();
        services.AddIssueCredentialFeatureApp();
        services.AddIssueCredentialFeatureInfra();
        services.AddGetCredentialFeatureInfra();
        services.AddSharedAdapters();
        return services.BuildServiceProvider(validateScopes: true);
    }

    private IssuanceRequest Pending(string name = "Ivan") => new(Guid.NewGuid(), OwnerName.Create(name),
        "Demo Authority", _time.GetUtcNow().AddHours(1));

    [Theory]
    [InlineData("Ivan", "IVAN")]
    [InlineData("Иван", "ИВАН")]
    public async Task ParallelCreation_ReservesOwnerExactlyOnce(string first, string second)
    {
        var create = _services.GetRequiredService<ITryCreateRequest>();
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(index =>
            Task.Run(() => create.Execute(Pending(index % 2 == 0 ? first : second)))));

        Assert.Equal(1, results.Count(created => created));
        Assert.Single(await _services.GetRequiredService<IGetPendingRequestIds>().Execute());
    }

    [Fact]
    public async Task Rejection_ReleasesOwnerButPreservesRejectedRequest()
    {
        var create = _services.GetRequiredService<ICreateRequestUseCase>();
        var first = (await create.Execute("Иван", _time.GetUtcNow().AddSeconds(5))).AsT0;
        _time.Advance(TimeSpan.FromSeconds(5));
        await _services.GetRequiredService<IIssueCredentialUseCase>().Execute(first.Id);

        var second = await create.Execute("ИВАН", _time.GetUtcNow().AddHours(1));

        Assert.True(second.IsT0);
        Assert.NotEqual(first.Id, second.AsT0.Id);
        var rejected = await _services.GetRequiredService<IFindRequestById>().Execute(first.Id);
        Assert.NotNull(rejected);
        Assert.Equal(RequestStatus.Rejected, rejected.Status);
        Assert.Equal(RejectionReason.ValidityExpired, rejected.RejectionReason);
        Assert.Null(rejected.SerialNumber);
        Assert.Equal("Иван", rejected.OwnerName.Value);
    }

    [Fact]
    public async Task IssuedCredential_RemainsImmutableAndReservesOwnerAfterExpiry()
    {
        var pending = Pending();
        await _services.GetRequiredService<ITryCreateRequest>().Execute(pending);
        var issue = _services.GetRequiredService<IIssueCredentialUseCase>();
        await issue.Execute(pending.Id);
        var find = _services.GetRequiredService<IFindRequestById>();
        var issued = (await find.Execute(pending.Id))!;
        var documents = _services.GetRequiredService<IFindCredential>();
        var original = await documents.Execute(issued.SerialNumber!.Value);

        _time.Advance(TimeSpan.FromDays(1));
        Assert.True((await issue.Execute(pending.Id)).IsT1);
        Assert.False(await _services.GetRequiredService<ITryCreateRequest>().Execute(Pending("IVAN")));

        Assert.Equal(issued, await find.Execute(pending.Id));
        Assert.Equal(original, await documents.Execute(issued.SerialNumber.Value));
    }

    [Fact]
    public async Task FailingCompletion_RollsBackBothDocumentAndStatusThenCanBeRetried()
    {
        var pending = Pending();
        await _services.GetRequiredService<ITryCreateRequest>().Execute(pending);
        await ExecuteSql("""
            CREATE TRIGGER fail_completion AFTER UPDATE ON requests
            WHEN NEW.status = 'Issued'
            BEGIN SELECT RAISE(ABORT, 'simulated failure'); END;
            """);

        var issue = _services.GetRequiredService<IIssueCredentialUseCase>();
        await Assert.ThrowsAsync<SqliteException>(() => issue.Execute(pending.Id));

        var stored = await _services.GetRequiredService<IFindRequestById>().Execute(pending.Id);
        Assert.NotNull(stored);
        Assert.Equal(RequestStatus.Pending, stored.Status);
        Assert.Null(stored.SerialNumber);
        await using (var connection = await _database.OpenConnection())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM requests WHERE serial_number IS NOT NULL OR issued_at IS NOT NULL;";
            Assert.Equal(0L, await command.ExecuteScalarAsync());
        }

        await ExecuteSql("DROP TRIGGER fail_completion;");
        Assert.True((await issue.Execute(pending.Id)).IsT0);
        Assert.True((await issue.Execute(pending.Id)).IsT1);
        var issued = (await _services.GetRequiredService<IFindRequestById>().Execute(pending.Id))!;
        Assert.NotNull(await _services.GetRequiredService<IFindCredential>().Execute(issued.SerialNumber!.Value));
    }

    [Fact]
    public async Task ConcurrentCompletion_PublishesOnlyOneCredential()
    {
        var request = Pending();
        await _services.GetRequiredService<ITryCreateRequest>().Execute(request);
        var completions = Enumerable.Range(0, 8).Select(_ => new Credential(Guid.NewGuid(), request.OwnerName,
            request.Issuer, _time.GetUtcNow(), request.ValidUntil)).ToArray();
        var save = _services.GetRequiredService<ICompleteRequest>();

        var results = await Task.WhenAll(completions.Select(document => Task.Run(() =>
            save.Execute(request.Issue(document.SerialNumber), document))));

        Assert.Single(results, result => result);
        var documents = _services.GetRequiredService<IFindCredential>();
        var published = await Task.WhenAll(completions.Select(document => documents.Execute(document.SerialNumber)));
        Assert.Single(published, document => document is not null);
    }

    [Fact]
    public async Task ReopeningDatabase_PreservesHistoryAndResumesPendingRequests()
    {
        var create = _services.GetRequiredService<ICreateRequestUseCase>();
        var first = (await create.Execute("Ivan", _time.GetUtcNow().AddHours(1))).AsT0;
        var second = (await create.Execute("Petr", _time.GetUtcNow().AddHours(1))).AsT0;
        await _services.GetRequiredService<IIssueCredentialUseCase>().Execute(first.Id);
        var issued = (await _services.GetRequiredService<IFindRequestById>().Execute(first.Id))!;
        var document = await _services.GetRequiredService<IFindCredential>().Execute(issued.SerialNumber!.Value);
        await _services.DisposeAsync();

        var reopened = new SqliteDatabase(Path.Combine(_directory, "test.db"));
        await reopened.Initialize();
        _services = CreateServices(reopened);

        Assert.Equal(issued, await _services.GetRequiredService<IFindRequestById>().Execute(first.Id));
        Assert.Equal(document, await _services.GetRequiredService<IFindCredential>().Execute(issued.SerialNumber.Value));
        Assert.Equal(new[] { second.Id }, await _services.GetRequiredService<IGetPendingRequestIds>().Execute());
        await _services.GetRequiredService<IProcessPendingRequestsUseCase>().Execute();
        Assert.Equal(RequestStatus.Issued,
            (await _services.GetRequiredService<IFindRequestById>().Execute(second.Id))!.Status);
    }

    private async Task ExecuteSql(string sql)
    {
        await using var connection = await _database.OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
