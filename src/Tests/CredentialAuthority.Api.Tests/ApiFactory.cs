using CredentialAuthority.Api.Workers.IssuanceWorker;
using CredentialAuthority.App.Features.IssueCredential;
using CredentialAuthority.Infra.Integrations.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

namespace CredentialAuthority.Api.Tests;

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath;

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));

    public ApiFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<DatabaseOptions>(options => options.Path = _databasePath);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
            var worker = services.Single(service => service.ServiceType == typeof(IHostedService)
                && service.ImplementationType == typeof(IssuanceWorker));
            services.Remove(worker);
        });
    }

    public async Task ProcessPendingRequests()
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IProcessPendingRequestsUseCase>().Execute();
    }
}
