using Microsoft.Extensions.Logging;

namespace CredentialAuthority.App.Features.IssueCredential;

public interface IProcessPendingRequestsUseCase
{
    Task Execute(CancellationToken ct = default);
}

public sealed class ProcessPendingRequestsUseCase : IProcessPendingRequestsUseCase
{
    private readonly IGetPendingRequestIds _getPendingIds;
    private readonly IIssueCredentialUseCase _issueCredential;
    private readonly ILogger<ProcessPendingRequestsUseCase> _logger;

    public ProcessPendingRequestsUseCase(IGetPendingRequestIds getPendingIds,
        IIssueCredentialUseCase issueCredential, ILogger<ProcessPendingRequestsUseCase> logger)
    {
        _getPendingIds = getPendingIds;
        _issueCredential = issueCredential;
        _logger = logger;
    }

    public async Task Execute(CancellationToken ct = default)
    {
        var ids = await _getPendingIds.Execute(ct);
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var result = await _issueCredential.Execute(id, ct);
                if (result.TryPickT0(out var completed, out _))
                {
                    _logger.LogInformation("Request {RequestId} completed with {Status}", id, completed.Status);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Processing request {RequestId} failed; it will be retried", id);
            }
        }
    }
}
