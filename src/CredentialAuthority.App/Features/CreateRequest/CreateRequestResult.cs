global using CreateRequestResult = OneOf.OneOf<
    CredentialAuthority.App.Features.CreateRequest.CreateRequestSuccess,
    CredentialAuthority.App.Features.CreateRequest.CreateRequestInvalid,
    CredentialAuthority.App.Features.CreateRequest.CreateRequestConflict>;

namespace CredentialAuthority.App.Features.CreateRequest;

public sealed record CreateRequestSuccess(Guid Id);
public sealed record CreateRequestInvalid(string Error);
public sealed record CreateRequestConflict;
