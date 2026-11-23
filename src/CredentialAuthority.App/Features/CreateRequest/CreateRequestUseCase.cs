using CredentialAuthority.App.Core.Models;
using Microsoft.Extensions.Options;

namespace CredentialAuthority.App.Features.CreateRequest;

public interface ICreateRequestUseCase
{
    Task<CreateRequestResult> Execute(string? ownerName, DateTimeOffset? validUntil,
        CancellationToken ct = default);
}

public sealed class CreateRequestUseCase : ICreateRequestUseCase
{
    private readonly ITryCreateRequest _createRequest;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<IssuerOptions> _issuer;

    public CreateRequestUseCase(ITryCreateRequest createRequest, TimeProvider timeProvider,
        IOptions<IssuerOptions> issuer)
    {
        _createRequest = createRequest;
        _timeProvider = timeProvider;
        _issuer = issuer;
    }

    public async Task<CreateRequestResult> Execute(string? ownerName, DateTimeOffset? validUntil,
        CancellationToken ct = default)
    {
        if (!OwnerName.TryCreate(ownerName, out var name))
        {
            return new CreateRequestInvalid("Owner name must start with a letter and contain 1–20 allowed characters.");
        }

        if (validUntil is null || validUntil <= _timeProvider.GetUtcNow())
        {
            return new CreateRequestInvalid("validUntil must be a date and time in the future.");
        }

        var request = new IssuanceRequest(Guid.NewGuid(), name, _issuer.Value.Name, validUntil.Value);
        return await _createRequest.Execute(request, ct)
            ? new CreateRequestSuccess(request.Id)
            : new CreateRequestConflict();
    }
}
