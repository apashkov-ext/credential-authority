using CredentialAuthority.App.Core.Models;
using CredentialAuthority.App.Features.IssueCredential;
using CredentialAuthority.App.SharedPorts;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Moq.AutoMock;

namespace CredentialAuthority.App.Tests;

public sealed class IssueCredentialUseCaseTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly Mock<IFindRequestById> _findRequest;
    private readonly Mock<ICompleteRequest> _complete;
    private readonly IssueCredentialUseCase _useCase;

    public IssueCredentialUseCaseTests()
    {
        var mocker = new AutoMocker();
        mocker.Use<TimeProvider>(_time);
        _findRequest = mocker.GetMock<IFindRequestById>();
        _complete = mocker.GetMock<ICompleteRequest>();
        _useCase = mocker.CreateInstance<IssueCredentialUseCase>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Processing_DecidesUsingActualIssueTime(int remainingSeconds)
    {
        var request = new IssuanceRequest(Guid.NewGuid(), OwnerName.Create("Ivan"), "Saved Issuer",
            _time.GetUtcNow().AddSeconds(remainingSeconds));
        _findRequest.Setup(x => x.Execute(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        IssuanceRequest? completion = null;
        Credential? document = null;
        _complete.Setup(x => x.Execute(It.IsAny<IssuanceRequest>(), It.IsAny<Credential?>(), It.IsAny<CancellationToken>()))
            .Callback<IssuanceRequest, Credential?, CancellationToken>((value, credential, _) =>
            {
                completion = value;
                document = credential;
            }).ReturnsAsync(true);

        var result = await _useCase.Execute(request.Id);

        Assert.True(result.IsT0);
        Assert.NotNull(completion);
        if (remainingSeconds <= 0)
        {
            Assert.Equal(RequestStatus.Rejected, completion.Status);
            Assert.Equal(RejectionReason.ValidityExpired, completion.RejectionReason);
            Assert.Null(document);
        }
        else
        {
            Assert.Equal(RequestStatus.Issued, completion.Status);
            Assert.NotNull(document);
            Assert.Equal(completion.SerialNumber, document.SerialNumber);
            Assert.Equal(request.Issuer, document.Issuer);
            Assert.Equal(request.OwnerName, document.OwnerName);
            Assert.Equal(request.ValidUntil, document.ValidUntil);
            Assert.Equal(_time.GetUtcNow(), document.IssuedAt);
        }
    }

    [Fact]
    public async Task CompletedOrMissingRequest_IsSkipped()
    {
        Assert.True((await _useCase.Execute(Guid.NewGuid())).IsT1);
        var request = new IssuanceRequest(Guid.NewGuid(), OwnerName.Create("Ivan"), "Demo Authority",
            _time.GetUtcNow().AddDays(1)).Reject(RejectionReason.ValidityExpired);
        _findRequest.Setup(x => x.Execute(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        Assert.True((await _useCase.Execute(request.Id)).IsT1);
        _complete.Verify(x => x.Execute(It.IsAny<IssuanceRequest>(), It.IsAny<Credential?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
