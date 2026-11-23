using CredentialAuthority.App.Core.Models;

namespace CredentialAuthority.Api.Features.GetRequest;

internal sealed record RequestResponse(Guid Id, string Status, string OwnerName, string Issuer,
    DateTimeOffset ValidUntil, string? CredentialUrl, string? RejectionReason)
{
    public static RequestResponse From(IssuanceRequest request) => new(
        request.Id, request.Status.ToString(), request.OwnerName.Value, request.Issuer, request.ValidUntil,
        request.SerialNumber is { } serial ? $"/credentials/{serial:D}" : null,
        request.RejectionReason?.ToString());
}
