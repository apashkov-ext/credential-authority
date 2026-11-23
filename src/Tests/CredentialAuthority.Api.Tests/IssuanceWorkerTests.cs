using System.Threading.Channels;
using CredentialAuthority.Api.Workers.IssuanceWorker;
using CredentialAuthority.App.Features.IssueCredential;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace CredentialAuthority.Api.Tests;

public sealed class IssuanceWorkerTests
{
    [Fact]
    public async Task Worker_WaitsTenSecondsBeforeFirstScanAndRepeatsEveryTenSeconds()
    {
        var time = new ObservedTimeProvider();
        var scan = new RecordingScan();
        await using var services = CreateServices(scan);
        using var worker = new IssuanceWorker(services.GetRequiredService<IServiceScopeFactory>(), time,
            NullLogger<IssuanceWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await time.TimerCreated.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, scan.Count);
        time.Advance(TimeSpan.FromSeconds(9));
        Assert.Equal(0, scan.Count);
        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, await scan.NextCall());
        time.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(2, await scan.NextCall());

        await worker.StopAsync(CancellationToken.None);
        Assert.Equal(1, scan.MaximumConcurrency);
    }

    [Fact]
    public async Task SlowScan_DoesNotOverlapOrReplayEveryMissedTick()
    {
        var time = new ObservedTimeProvider();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scan = new RecordingScan(gate.Task);
        await using var services = CreateServices(scan);
        using var worker = new IssuanceWorker(services.GetRequiredService<IServiceScopeFactory>(), time,
            NullLogger<IssuanceWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await time.TimerCreated.WaitAsync(TimeSpan.FromSeconds(5));
        time.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(1, await scan.NextCall());

        time.Advance(TimeSpan.FromSeconds(30));
        Assert.Equal(1, scan.Count);
        gate.SetResult();
        Assert.Equal(2, await scan.NextCall());

        await worker.StopAsync(CancellationToken.None);
        Assert.Equal(2, scan.Count);
        Assert.Equal(1, scan.MaximumConcurrency);
    }

    private static ServiceProvider CreateServices(RecordingScan scan)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IProcessPendingRequestsUseCase>(scan);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private sealed class RecordingScan : IProcessPendingRequestsUseCase
    {
        private readonly Task? _firstCallGate;
        private readonly Channel<int> _calls = Channel.CreateUnbounded<int>();
        private int _count;
        private int _active;

        public int Count => Volatile.Read(ref _count);
        public int MaximumConcurrency { get; private set; }

        public RecordingScan(Task? firstCallGate = null)
        {
            _firstCallGate = firstCallGate;
        }

        public async Task Execute(CancellationToken ct = default)
        {
            var active = Interlocked.Increment(ref _active);
            MaximumConcurrency = Math.Max(MaximumConcurrency, active);
            var index = Interlocked.Increment(ref _count);
            _calls.Writer.TryWrite(index);
            try
            {
                if (index == 1 && _firstCallGate is not null)
                {
                    await _firstCallGate.WaitAsync(ct);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }

        public Task<int> NextCall() => _calls.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class ObservedTimeProvider : TimeProvider
    {
        private readonly FakeTimeProvider _time = new();
        private readonly TaskCompletionSource _timerCreated = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task TimerCreated => _timerCreated.Task;
        public override DateTimeOffset GetUtcNow() => _time.GetUtcNow();
        public void Advance(TimeSpan value) => _time.Advance(value);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = _time.CreateTimer(callback, state, dueTime, period);
            _timerCreated.TrySetResult();
            return timer;
        }
    }
}
