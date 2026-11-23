using CredentialAuthority.App.SharedPorts;

namespace CredentialAuthority.App.Features.GetRequest;

public interface IGetRequestUseCase
{
    Task<GetRequestResult> Execute(Guid id, CancellationToken ct = default);
}

public sealed class GetRequestUseCase : IGetRequestUseCase
{
    private readonly IFindRequestById _findRequest;

    public GetRequestUseCase(IFindRequestById findRequest)
    {
        _findRequest = findRequest;
    }

    public async Task<GetRequestResult> Execute(Guid id, CancellationToken ct = default)
    {
        var request = await _findRequest.Execute(id, ct);
        return request is null ? new GetRequestNotFound() : request;
    }
}
