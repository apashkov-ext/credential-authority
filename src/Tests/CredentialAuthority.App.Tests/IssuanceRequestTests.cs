using CredentialAuthority.App.Core.Models;

namespace CredentialAuthority.App.Tests;

public sealed class IssuanceRequestTests
{
    private static IssuanceRequest Pending() => new(Guid.NewGuid(), OwnerName.Create("Ivan"),
        "Demo Authority", new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void IssuedAndRejectedRequests_CannotChangeState()
    {
        var pending = Pending();
        var issued = pending.Issue(Guid.NewGuid());
        var rejected = pending.Reject(RejectionReason.ValidityExpired);

        Assert.Equal(RequestStatus.Pending, pending.Status);
        Assert.Throws<InvalidOperationException>(() => issued.Issue(Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => issued.Reject(RejectionReason.ValidityExpired));
        Assert.Throws<InvalidOperationException>(() => rejected.Issue(Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => rejected.Reject(RejectionReason.ValidityExpired));
    }

    [Fact]
    public void ValidityBoundary_IsExpiredAtExactDeadline()
    {
        var request = Pending();
        Assert.False(request.HasExpired(request.ValidUntil.AddTicks(-1)));
        Assert.True(request.HasExpired(request.ValidUntil));
        Assert.True(request.HasExpired(request.ValidUntil.AddTicks(1)));
    }

    [Fact]
    public void Credential_CannotBeIssuedAtOrAfterItsDeadline()
    {
        var request = Pending();
        Assert.Throws<ArgumentException>(() => new Credential(Guid.NewGuid(), request.OwnerName,
            request.Issuer, request.ValidUntil, request.ValidUntil));
    }
}
