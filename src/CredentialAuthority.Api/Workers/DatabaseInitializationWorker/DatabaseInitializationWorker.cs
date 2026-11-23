using CredentialAuthority.Infra.Integrations.Sqlite;

namespace CredentialAuthority.Api.Workers.DatabaseInitializationWorker;

internal sealed class DatabaseInitializationWorker : BackgroundService
{
    private readonly SqliteDatabase _database;
    private readonly ILogger<DatabaseInitializationWorker> _logger;

    public DatabaseInitializationWorker(SqliteDatabase database, ILogger<DatabaseInitializationWorker> logger)
    {
        _database = database;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        // In .NET 10 ExecuteAsync runs on the thread pool; explicitly await this one-time prerequisite.
        await ExecuteTask!.WaitAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Initializing database");
        await _database.Initialize(stoppingToken);
        _logger.LogInformation("Database initialized");
    }
}
