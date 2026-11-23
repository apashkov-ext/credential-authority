using CredentialAuthority.Api.Workers.DatabaseInitializationWorker;
using CredentialAuthority.Infra.Integrations.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace CredentialAuthority.Api.Tests;

public sealed class DatabaseInitializationWorkerTests
{
    [Fact]
    public async Task Start_WaitsForSchemaAndCompletesOneTimeWorker()
    {
        var directory = Path.Combine(Path.GetTempPath(), "credential-authority-init", Guid.NewGuid().ToString("N"));
        var database = new SqliteDatabase(Path.Combine(directory, "test.db"));
        try
        {
            using var worker = new DatabaseInitializationWorker(database,
                NullLogger<DatabaseInitializationWorker>.Instance);

            await worker.StartAsync(CancellationToken.None);

            Assert.NotNull(worker.ExecuteTask);
            Assert.True(worker.ExecuteTask.IsCompletedSuccessfully);
            await using var connection = await database.OpenConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM requests;";
            Assert.Equal(0L, await command.ExecuteScalarAsync());
            await worker.StopAsync(CancellationToken.None);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InitializationFailure_PreventsApiStartup()
    {
        var directory = Path.Combine(Path.GetTempPath(), "credential-authority-init", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            // SQLite cannot open a directory as its database file.
            await using var factory = new ApiFactory(directory);

            Assert.Throws<SqliteException>(() => factory.CreateClient());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
