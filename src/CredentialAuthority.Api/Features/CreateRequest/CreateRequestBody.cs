namespace CredentialAuthority.Api.Features.CreateRequest;

internal sealed record CreateRequestBody(string? OwnerName, DateTimeOffset? ValidUntil);
internal sealed record CreateRequestResponse(Guid Id);
