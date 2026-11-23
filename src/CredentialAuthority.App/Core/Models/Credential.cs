namespace CredentialAuthority.App.Core.Models;

public sealed record Credential
{
    public Guid SerialNumber { get; }
    public OwnerName OwnerName { get; }
    public string Issuer { get; }
    public DateTimeOffset IssuedAt { get; }
    public DateTimeOffset ValidUntil { get; }

    public Credential(Guid serialNumber, OwnerName ownerName, string issuer,
        DateTimeOffset issuedAt, DateTimeOffset validUntil)
    {
        if (serialNumber == Guid.Empty)
        {
            throw new ArgumentException("Serial number must not be empty.", nameof(serialNumber));
        }

        ArgumentNullException.ThrowIfNull(ownerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        if (issuedAt >= validUntil)
        {
            throw new ArgumentException("The credential must expire after its issue time.", nameof(validUntil));
        }

        SerialNumber = serialNumber;
        OwnerName = ownerName;
        Issuer = issuer;
        IssuedAt = issuedAt.ToUniversalTime();
        ValidUntil = validUntil.ToUniversalTime();
    }
}
