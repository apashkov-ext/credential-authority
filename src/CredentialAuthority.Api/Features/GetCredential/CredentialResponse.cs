using CredentialAuthority.App.Core.Models;

namespace CredentialAuthority.Api.Features.GetCredential;

internal sealed record CredentialResponse(Guid SerialNumber, string OwnerName, string Issuer,
    DateTimeOffset IssuedAt, DateTimeOffset ValidUntil)
{
    public static CredentialResponse From(Credential credential) => new(
        credential.SerialNumber, credential.OwnerName.Value, credential.Issuer,
        credential.IssuedAt, credential.ValidUntil);
}
