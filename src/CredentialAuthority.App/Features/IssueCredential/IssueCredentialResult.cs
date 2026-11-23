global using IssueCredentialResult = OneOf.OneOf<
    CredentialAuthority.App.Features.IssueCredential.IssueCredentialCompleted,
    CredentialAuthority.App.Features.IssueCredential.IssueCredentialSkipped>;

using CredentialAuthority.App.Core.Models;

namespace CredentialAuthority.App.Features.IssueCredential;

public sealed record IssueCredentialCompleted(Guid Id, RequestStatus Status);
public sealed record IssueCredentialSkipped;
