namespace CredentialAuthority.App.Core.Models;

public sealed record IssuanceRequest
{
    public Guid Id { get; }
    public OwnerName OwnerName { get; }
    public string Issuer { get; }
    public DateTimeOffset ValidUntil { get; }
    public RequestStatus Status { get; private init; }
    public Guid? SerialNumber { get; private init; }
    public RejectionReason? RejectionReason { get; private init; }

    public IssuanceRequest(Guid id, OwnerName ownerName, string issuer, DateTimeOffset validUntil)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Request ID must not be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(ownerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        Id = id;
        OwnerName = ownerName;
        Issuer = issuer;
        ValidUntil = validUntil.ToUniversalTime();
        Status = RequestStatus.Pending;
    }

    public bool HasExpired(DateTimeOffset now) => now >= ValidUntil;

    public IssuanceRequest Issue(Guid serialNumber)
    {
        EnsurePending();
        if (serialNumber == Guid.Empty)
        {
            throw new ArgumentException("Serial number must not be empty.", nameof(serialNumber));
        }

        return this with { Status = RequestStatus.Issued, SerialNumber = serialNumber };
    }

    public IssuanceRequest Reject(RejectionReason reason)
    {
        EnsurePending();
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return this with { Status = RequestStatus.Rejected, RejectionReason = reason };
    }

    private void EnsurePending()
    {
        if (Status != RequestStatus.Pending)
        {
            throw new InvalidOperationException("A completed request cannot change its state.");
        }
    }
}
