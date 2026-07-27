using System.Diagnostics;
using System.Reflection;
using Art.Infra.Extensions;
using Art.Infra.Framework.Jobs;
using FreeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Scheduler = Art.Infra.Framework.Jobs.TaskScheduler;

namespace Art.Tests.Framework.Jobs;

public class TaskSchedulerTests
{
    [Fact]
    public void AddTaskScheduler_MapsAllServicesToOneInstance()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AutoDependencyInjection();
        builder.Services.AddTaskScheduler();

        using var host = builder.Build();
        var scheduler = host.Services.GetRequiredService<Scheduler>();
        var schedulerServices = host.Services.GetServices<ITaskScheduler>().ToArray();
        var hostedSchedulers = host.Services.GetServices<IHostedService>().OfType<Scheduler>().ToArray();

        Assert.Single(schedulerServices);
        Assert.Single(hostedSchedulers);
        Assert.Same(scheduler, schedulerServices[0]);
        Assert.Same(scheduler, hostedSchedulers[0]);
    }

    [Fact]
    public async Task InfrastructureFailure_DoesNotStopTheNextTick()
    {
        using var redis = new RedisClient("127.0.0.1:6379");
        using var services = new ServiceCollection()
            .AddSingleton(redis)
            .BuildServiceProvider();
        var lifetime = new TestApplicationLifetime();
        var scheduler = new FailOnceLockScheduler(lifetime, services);
        var executed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var taskName = FindTaskNameWithoutInitialDelay(scheduler);

        scheduler.AddRecurringTask(
            _ =>
            {
                executed.TrySetResult();
                return Task.CompletedTask;
            },
            TimeSpan.FromMilliseconds(10),
            taskName: taskName);

        await scheduler.StartAsync(CancellationToken.None);
        await executed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await scheduler.StopAsync(stopTimeout.Token);

        Assert.True(scheduler.LockAttempts >= 2);
    }

    [Fact]
    public async Task StopAsync_CancelsTheRunningTaskAndWaitsForIt()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var lifetime = new TestApplicationLifetime();
        var scheduler = new Scheduler(NullLogger<Scheduler>.Instance, lifetime, services);
        var actionStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var taskName = FindTaskNameWithoutInitialDelay(scheduler);

        scheduler.AddRecurringTask(
            async token =>
            {
                actionStarted.TrySetResult(token);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            },
            TimeSpan.FromMilliseconds(10),
            useDistributedLock: false,
            taskName: taskName);

        await scheduler.StartAsync(CancellationToken.None);
        var actionToken = await actionStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await scheduler.StopAsync(stopTimeout.Token);

        Assert.True(actionToken.IsCancellationRequested);
    }

    [Fact]
    public async Task LongRunningTask_EndsTheWindowAndWaitsBeforeReacquiringTheLock()
    {
        using var redis = new RedisClient("127.0.0.1:6379");
        using var services = new ServiceCollection()
            .AddSingleton(redis)
            .BuildServiceProvider();
        var lifetime = new TestApplicationLifetime();
        var scheduler = new TrackingLockScheduler(lifetime, services);
        var firstWindowEnded = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var taskName = FindTaskNameWithoutInitialDelay(scheduler);
        long firstWindowEndedTimestamp = 0;
        var invocationCount = 0;

        scheduler.AddLongRunningTask(
            async token =>
            {
                try
                {
                    Interlocked.Increment(ref invocationCount);
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                }
                finally
                {
                    if (Interlocked.CompareExchange(ref firstWindowEndedTimestamp, Stopwatch.GetTimestamp(), 0) == 0)
                        firstWindowEnded.TrySetResult(token);
                }
            },
            interval: TimeSpan.FromMilliseconds(120),
            processingInterval: TimeSpan.Zero,
            runDuration: TimeSpan.FromMilliseconds(80),
            taskName: taskName);

        await scheduler.StartAsync(CancellationToken.None);
        try
        {
            var windowToken = await firstWindowEnded.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await scheduler.SecondLockAttempt.WaitAsync(TimeSpan.FromSeconds(2));

            var reacquireDelay = Stopwatch.GetElapsedTime(
                Volatile.Read(ref firstWindowEndedTimestamp),
                scheduler.SecondLockAttemptTimestamp);

            Assert.True(windowToken.IsCancellationRequested);
            Assert.True(invocationCount >= 1);
            Assert.True(
                reacquireDelay >= TimeSpan.FromMilliseconds(100),
                $"锁释放后仅等待了 {reacquireDelay.TotalMilliseconds:F0}ms，未满足完整 interval");
        }
        finally
        {
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await scheduler.StopAsync(stopTimeout.Token);
        }
    }

    private static string FindTaskNameWithoutInitialDelay(Scheduler scheduler)
    {
        var calculateDelay = typeof(Scheduler).GetMethod(
            "CalculateInitialDelay",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        for (var index = 0; index < 1_000; index++)
        {
            var taskName = $"test.task.{index}";
            var delay = (TimeSpan)calculateDelay.Invoke(scheduler, [taskName])!;
            if (delay == TimeSpan.Zero) return taskName;
        }

        throw new InvalidOperationException("无法生成零初始延迟的测试任务名");
    }

    private sealed class FailOnceLockScheduler : Scheduler
    {
        private int _lockAttempts;

        public FailOnceLockScheduler(IHostApplicationLifetime lifetime, IServiceProvider serviceProvider)
            : base(NullLogger<Scheduler>.Instance, lifetime, serviceProvider)
        {
        }

        public int LockAttempts => _lockAttempts;

        internal override Task<IAsyncDisposable?> TryAcquireDistributedLockAsync(TaskInfo task)
        {
            if (Interlocked.Increment(ref _lockAttempts) == 1)
                throw new InvalidOperationException("模拟 Redis 短暂故障");

            return Task.FromResult<IAsyncDisposable?>(new NoopAsyncDisposable());
        }
    }

    private sealed class TrackingLockScheduler : Scheduler
    {
        private readonly TaskCompletionSource _secondLockAttempt = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _lockAttempts;

        public TrackingLockScheduler(IHostApplicationLifetime lifetime, IServiceProvider serviceProvider)
            : base(NullLogger<Scheduler>.Instance, lifetime, serviceProvider)
        {
        }

        public Task SecondLockAttempt => _secondLockAttempt.Task;
        public long SecondLockAttemptTimestamp { get; private set; }

        internal override Task<IAsyncDisposable?> TryAcquireDistributedLockAsync(TaskInfo task)
        {
            if (Interlocked.Increment(ref _lockAttempts) == 2)
            {
                SecondLockAttemptTimestamp = Stopwatch.GetTimestamp();
                _secondLockAttempt.TrySetResult();
            }

            return Task.FromResult<IAsyncDisposable?>(new NoopAsyncDisposable());
        }
    }

    private sealed class NoopAsyncDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TestApplicationLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource _started = new();
        private readonly CancellationTokenSource _stopping = new();
        private readonly CancellationTokenSource _stopped = new();

        public CancellationToken ApplicationStarted => _started.Token;
        public CancellationToken ApplicationStopping => _stopping.Token;
        public CancellationToken ApplicationStopped => _stopped.Token;

        public void StopApplication() => _stopping.Cancel();
    }
}
