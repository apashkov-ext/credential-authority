using CredentialAuthority.App.Core.Models;
using CredentialAuthority.App.Features.IssueCredential;
using Moq;
using Moq.AutoMock;

namespace CredentialAuthority.App.Tests;

public sealed class ProcessPendingRequestsUseCaseTests
{
    [Fact]
    public async Task OneRequestFails_OthersStillCompleteAndFailureIsRetriedOnNextScan()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var mocker = new AutoMocker();
        mocker.GetMock<IGetPendingRequestIds>().SetupSequence(x => x.Execute(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { first, second }).ReturnsAsync(new[] { first });
        var processed = new List<Guid>();
        mocker.GetMock<IIssueCredentialUseCase>().SetupSequence(x => x.Execute(first, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Temporary storage failure"))
            .ReturnsAsync(new IssueCredentialCompleted(first, RequestStatus.Issued));
        mocker.GetMock<IIssueCredentialUseCase>().Setup(x => x.Execute(second, It.IsAny<CancellationToken>()))
            .Callback(() => processed.Add(second))
            .ReturnsAsync(new IssueCredentialCompleted(second, RequestStatus.Issued));
        var useCase = mocker.CreateInstance<ProcessPendingRequestsUseCase>();

        await useCase.Execute();
        Assert.Equal(new[] { second }, processed);
        await useCase.Execute();

        mocker.GetMock<IIssueCredentialUseCase>().Verify(x => x.Execute(first,
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ShutdownCancellation_IsPropagatedWithoutProcessingNextRequest()
    {
        var mocker = new AutoMocker();
        using var cancellation = new CancellationTokenSource();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        mocker.GetMock<IGetPendingRequestIds>().Setup(x => x.Execute(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { first, second });
        mocker.GetMock<IIssueCredentialUseCase>().Setup(x => x.Execute(first, It.IsAny<CancellationToken>()))
            .Callback(() => cancellation.Cancel())
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mocker.CreateInstance<ProcessPendingRequestsUseCase>().Execute(cancellation.Token));

        mocker.GetMock<IIssueCredentialUseCase>().Verify(x => x.Execute(second,
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
