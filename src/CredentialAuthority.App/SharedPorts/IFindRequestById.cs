using CredentialAuthority.App.Core.Models;

namespace CredentialAuthority.App.SharedPorts;

public interface IFindRequestById
{
    Task<IssuanceRequest?> Execute(Guid id, CancellationToken ct = default);
}
