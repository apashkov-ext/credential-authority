using CredentialAuthority.App.Core.Models;
using CredentialAuthority.App.SharedPorts;

namespace CredentialAuthority.App.Features.IssueCredential;

public interface IIssueCredentialUseCase
{
    Task<IssueCredentialResult> Execute(Guid id, CancellationToken ct = default);
}

public sealed class IssueCredentialUseCase : IIssueCredentialUseCase
{
    private readonly IFindRequestById _findRequest;
    private readonly ICompleteRequest _completeRequest;
    private readonly TimeProvider _timeProvider;

    public IssueCredentialUseCase(IFindRequestById findRequest, ICompleteRequest completeRequest,
        TimeProvider timeProvider)
    {
        _findRequest = findRequest;
        _completeRequest = completeRequest;
        _timeProvider = timeProvider;
    }

    public async Task<IssueCredentialResult> Execute(Guid id, CancellationToken ct = default)
    {
        var request = await _findRequest.Execute(id, ct);
        if (request is null || request.Status != RequestStatus.Pending)
        {
            return new IssueCredentialSkipped();
        }

        var now = _timeProvider.GetUtcNow();
        Credential? credential = null;
        if (request.HasExpired(now))
        {
            request = request.Reject(RejectionReason.ValidityExpired);
        }
        else
        {
            credential = new Credential(Guid.NewGuid(), request.OwnerName, request.Issuer, now, request.ValidUntil);
            request = request.Issue(credential.SerialNumber);
        }

        return await _completeRequest.Execute(request, credential, ct)
            ? new IssueCredentialCompleted(id, request.Status)
            : new IssueCredentialSkipped();
    }
}
