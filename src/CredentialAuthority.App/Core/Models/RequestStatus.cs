namespace CredentialAuthority.App.Core.Models;

public enum RequestStatus
{
    Pending,
    Issued,
    Rejected
}

public enum RejectionReason
{
    ValidityExpired
}
