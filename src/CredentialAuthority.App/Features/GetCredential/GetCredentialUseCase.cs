namespace CredentialAuthority.App.Features.GetCredential;

public interface IGetCredentialUseCase
{
    Task<GetCredentialResult> Execute(Guid serialNumber, CancellationToken ct = default);
}

public sealed class GetCredentialUseCase : IGetCredentialUseCase
{
    private readonly IFindCredential _findCredential;

    public GetCredentialUseCase(IFindCredential findCredential)
    {
        _findCredential = findCredential;
    }

    public async Task<GetCredentialResult> Execute(Guid serialNumber, CancellationToken ct = default)
    {
        var credential = await _findCredential.Execute(serialNumber, ct);
        return credential is null ? new GetCredentialNotFound() : credential;
    }
}
