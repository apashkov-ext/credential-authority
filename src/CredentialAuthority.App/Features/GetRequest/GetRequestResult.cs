global using GetRequestResult = OneOf.OneOf<
    CredentialAuthority.App.Core.Models.IssuanceRequest,
    CredentialAuthority.App.Features.GetRequest.GetRequestNotFound>;

namespace CredentialAuthority.App.Features.GetRequest;

public sealed record GetRequestNotFound;
