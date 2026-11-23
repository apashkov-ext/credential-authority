using CredentialAuthority.App.Core.Models;

namespace CredentialAuthority.App.Features.GetCredential;

public interface IFindCredential
{
    Task<Credential?> Execute(Guid serialNumber, CancellationToken ct = default);
}
