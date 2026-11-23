using CredentialAuthority.App.Features.IssueCredential;

namespace CredentialAuthority.Api.Workers.IssuanceWorker;

internal sealed class IssuanceWorker : BackgroundService
{
    public static readonly TimeSpan Period = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IssuanceWorker> _logger;

    public IssuanceWorker(IServiceScopeFactory scopeFactory, TimeProvider timeProvider,
        ILogger<IssuanceWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Period, _timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IProcessPendingRequestsUseCase>()
                        .Execute(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Pending request scan failed; retrying on the next tick");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown must not turn unfinished requests into business rejections.
        }
    }
}
