global using GetCredentialResult = OneOf.OneOf<
    CredentialAuthority.App.Core.Models.Credential,
    CredentialAuthority.App.Features.GetCredential.GetCredentialNotFound>;

namespace CredentialAuthority.App.Features.GetCredential;

public sealed record GetCredentialNotFound;
