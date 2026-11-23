using CredentialAuthority.App.Core.Models;
using CredentialAuthority.App.Features.CreateRequest;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Moq.AutoMock;

namespace CredentialAuthority.App.Tests;

public sealed class CreateRequestUseCaseTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly Mock<ITryCreateRequest> _createRequest;
    private readonly CreateRequestUseCase _useCase;

    public CreateRequestUseCaseTests()
    {
        var mocker = new AutoMocker();
        mocker.Use<TimeProvider>(_time);
        mocker.Use(Options.Create(new IssuerOptions()));
        _createRequest = mocker.GetMock<ITryCreateRequest>();
        _useCase = mocker.CreateInstance<CreateRequestUseCase>();
    }

    [Fact]
    public async Task ValidInput_CreatesPendingRequestWithServerIssuerAndUtcDeadline()
    {
        IssuanceRequest? saved = null;
        _createRequest.Setup(x => x.Execute(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<IssuanceRequest, CancellationToken>((request, _) => saved = request)
            .ReturnsAsync(true);
        var until = _time.GetUtcNow().AddHours(1).ToOffset(TimeSpan.FromHours(3));

        var result = await _useCase.Execute("Иван", until);

        Assert.True(result.IsT0);
        Assert.NotNull(saved);
        Assert.Equal(result.AsT0.Id, saved.Id);
        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.Equal(RequestStatus.Pending, saved.Status);
        Assert.Equal("Иван", saved.OwnerName.Value);
        Assert.Equal("Demo Authority", saved.Issuer);
        Assert.Equal(until.UtcDateTime, saved.ValidUntil.UtcDateTime);
        Assert.Equal(TimeSpan.Zero, saved.ValidUntil.Offset);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task DeadlineNotInFuture_IsRejectedBeforeSaving(int seconds)
    {
        var result = await _useCase.Execute("Ivan", _time.GetUtcNow().AddSeconds(seconds));

        Assert.True(result.IsT1);
        _createRequest.Verify(x => x.Execute(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingFields_AreInvalid()
    {
        Assert.True((await _useCase.Execute(null, _time.GetUtcNow().AddDays(1))).IsT1);
        Assert.True((await _useCase.Execute("Ivan", null)).IsT1);
    }

    [Fact]
    public async Task ReservedOwner_ReturnsConflict()
    {
        _createRequest.Setup(x => x.Execute(It.IsAny<IssuanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Assert.True((await _useCase.Execute("Ivan", _time.GetUtcNow().AddDays(1))).IsT2);
    }
}
