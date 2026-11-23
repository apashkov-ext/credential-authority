namespace CredentialAuthority.App.Features.IssueCredential;

public interface IGetPendingRequestIds
{
    Task<IReadOnlyList<Guid>> Execute(CancellationToken ct = default);
}
