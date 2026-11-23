using CredentialAuthority.App.Core.Models;

namespace CredentialAuthority.App.Features.CreateRequest;

public interface ITryCreateRequest
{
    // Returns false only when a Pending/Issued request already reserves this owner.
    Task<bool> Execute(IssuanceRequest request, CancellationToken ct = default);
}
