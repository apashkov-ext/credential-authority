using CredentialAuthority.App.Core.Models;

namespace CredentialAuthority.App.Features.IssueCredential;

public interface ICompleteRequest
{
    // Atomically saves the completion only if the stored request is still Pending.
    Task<bool> Execute(IssuanceRequest request, Credential? credential, CancellationToken ct = default);
}
