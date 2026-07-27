using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FreeRedis;
using Art.Infra.Cache;

namespace Art.Infra.Framework.Jobs;

/// <summary>
/// 任务信息
/// </summary>
internal class TaskInfo
{
    public string Name { get; set; } = default!;
    public Func<CancellationToken, Task> Action { get; set; } = default!;
    public TaskType Type { get; set; }
    public TimeSpan Interval { get; set; }
    public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromMilliseconds(50);
    public TimeSpan RunDuration { get; set; } = TimeSpan.FromMinutes(1);
    public int[]? AllowedHours { get; set; }
    public TimeSpan? PreventDuplicateInterval { get; set; }
    public bool UseDistributedLock { get; set; } = true;
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(2);
}

internal enum TaskType
{
    Recurring,
    LongRunning
}

/// <summary>
/// 安全延迟扩展方法
/// </summary>
internal static class TaskExtensions
{
    /// <summary>
    /// 安全延迟，返回 true 表示正常完成，返回 false 表示被取消
    /// </summary>
    public static async Task<bool> SafeDelay(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}

/// <summary>
/// 任务调度器实现
/// </summary>
public class TaskScheduler : ITaskScheduler, IHostedService
{
    private readonly ILogger<TaskScheduler> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IServiceProvider _serviceProvider;

    private readonly List<TaskInfo> _tasks = new();
    private readonly List<Task> _runningTasks = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly string _podId = $"{Environment.MachineName}-{Environment.ProcessId}";
    private RedisClient? _redisClient;

    public TaskScheduler(
        ILogger<TaskScheduler> logger,
        IHostApplicationLifetime lifetime,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _lifetime = lifetime;
        _serviceProvider = serviceProvider;
    }

    public void AddRecurringTask(
        Func<CancellationToken, Task> taskAction,
        TimeSpan interval,
        int[]? allowedHours = null,
        TimeSpan? preventDuplicateInterval = null,
        bool useDistributedLock = true,
        string? taskName = null)
    {
        var name = taskName ?? GenerateTaskName(taskAction);
        _tasks.Add(new TaskInfo
        {
            Name = name,
            Action = taskAction,
            Type = TaskType.Recurring,
            Interval = interval,
            AllowedHours = allowedHours,
            PreventDuplicateInterval = preventDuplicateInterval,
            UseDistributedLock = useDistributedLock,
            LockTimeout = CalculateLockTimeout(interval)
        });
    }

    public void AddLongRunningTask(
        Func<CancellationToken, Task> taskAction,
        TimeSpan interval,
        TimeSpan? processingInterval = null,
        TimeSpan? runDuration = null,
        bool useDistributedLock = true,
        string? taskName = null)
    {
        var name = taskName ?? GenerateTaskName(taskAction);
        _tasks.Add(new TaskInfo
        {
            Name = name,
            Action = taskAction,
            Type = TaskType.LongRunning,
            Interval = interval,
            ProcessingInterval = processingInterval ?? TimeSpan.FromMilliseconds(50),
            RunDuration = runDuration ?? TimeSpan.FromMinutes(1),
            UseDistributedLock = useDistributedLock,
            LockTimeout = CalculateLockTimeout(runDuration ?? TimeSpan.FromMinutes(1))
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("任务调度器启动 - Pod: {PodId}", _podId);

        _redisClient = _serviceProvider.GetService<RedisClient>();
        if (_redisClient == null)
            _logger.LogWarning("未检测到 Redis 连接，分布式锁将被禁用");

        // 配置任务
        ConfigureDefaultTasks();

        foreach (var task in _tasks)
        {
            var runner = RunTaskLoop(task);
            _runningTasks.Add(runner);
            _logger.LogInformation("任务已注册: {TaskName}, 间隔: {Interval}", task.Name, task.Interval);
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("任务调度器停止中...");
        _cts.Cancel();

        var allTasks = Task.WhenAll(_runningTasks);
        try
        {
            await allTasks.WaitAsync(cancellationToken);
            _logger.LogInformation("所有任务已正常停止");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var unfinishedCount = _runningTasks.Count(task => !task.IsCompleted);
            _logger.LogWarning("等待任务停止超时，仍有 {Count} 个任务未完成", unfinishedCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("所有任务已响应取消信号");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "等待任务停止时发生异常");
        }
    }

    private void ConfigureDefaultTasks()
    {
        if (_tasks.Count > 0) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider.GetService<ITaskConfigurationProvider>();
            provider?.ConfigureTasks(this);

            _logger.LogInformation("任务配置完成，共 {Count} 个任务", _tasks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置任务失败");
        }
    }

    /// <summary>
    /// 执行任务（带安全检查）
    /// <para>
    /// loopToken 同时控制调度循环和业务任务，业务代码应在批次或事务边界响应取消。
    /// </para>
    /// </summary>
    private async Task ExecuteTaskSafe(TaskInfo task, CancellationToken loopToken)
    {
        // 检查时间限制
        if (task.AllowedHours != null && !task.AllowedHours.Contains(DateTime.Now.Hour))
            return;

        if (task.PreventDuplicateInterval.HasValue)
        {
            var ok = await TryAcquireDedupAsync(task);
            if (!ok) return;
        }

        if (task.UseDistributedLock && _redisClient != null)
        {
            await using var locker = await TryAcquireDistributedLockAsync(task);
            if (locker == null) return;

            await ExecuteByTypeAsync(task, loopToken);
            return;
        }

        await ExecuteByTypeAsync(task, loopToken);
    }

    private static string GenerateTaskName(Func<CancellationToken, Task> action)
    {
        var method = action.Method;
        return $"{method.DeclaringType?.Name}.{method.Name}";
    }

    private TimeSpan CalculateInitialDelay(string taskName)
    {
        // 根据任务名 hash 计算初始延迟，让任务分散执行
        var hash = Math.Abs((taskName + _podId).GetHashCode());
        var delaySeconds = hash % 31;
        return TimeSpan.FromSeconds(delaySeconds);
    }

    private TimeSpan CalculateLockTimeout(TimeSpan taskDuration)
    {
        var baseSeconds = (int)(taskDuration.TotalSeconds * 1.5);
        var seconds = Math.Max(30, Math.Min(baseSeconds, 300));
        return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// 长期任务运行窗口
    /// <para>
    /// 窗口 Token 会传入业务任务；业务任务应完成当前最小工作单元后响应取消。
    /// </para>
    /// </summary>
    private async Task RunLongRunningWindowAsync(TaskInfo task, CancellationToken loopToken)
    {
        using var windowCts = CancellationTokenSource.CreateLinkedTokenSource(loopToken);
        windowCts.CancelAfter(task.RunDuration);
        var windowToken = windowCts.Token;
        var processedCount = 0;

        while (!windowToken.IsCancellationRequested)
        {
            try
            {
                await task.Action(windowToken);
                processedCount++;
            }
            catch (OperationCanceledException) when (windowToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "长期任务单次执行失败: {TaskName}", task.Name);
            }

            if (task.ProcessingInterval > TimeSpan.Zero)
            {
                if (!await TaskExtensions.SafeDelay(task.ProcessingInterval, windowToken))
                    break;
            }
        }

        if (loopToken.IsCancellationRequested)
            _logger.LogInformation("长期任务 {TaskName} 已响应停止信号，共处理 {Count} 次", task.Name, processedCount);
        else
            _logger.LogDebug("长期任务 {TaskName} 运行窗口结束，共处理 {Count} 次", task.Name, processedCount);
    }

    /// <summary>
    /// 执行一次调度并隔离异常，返回 false 表示调度循环应停止。
    /// </summary>
    private async Task<bool> ExecuteScheduledIterationAsync(TaskInfo task, CancellationToken loopToken)
    {
        try
        {
            await ExecuteTaskSafe(task, loopToken);
            return true;
        }
        catch (OperationCanceledException) when (loopToken.IsCancellationRequested)
        {
            _logger.LogInformation("任务 {TaskName} 已响应停止信号", task.Name);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "任务调度执行失败，将在下一周期重试: {TaskName}", task.Name);
            return true;
        }
    }

    private async Task RunRecurringScheduleAsync(TaskInfo task, CancellationToken loopToken)
    {
        using var timer = new PeriodicTimer(task.Interval);
        while (await timer.WaitForNextTickAsync(loopToken))
        {
            if (!await ExecuteScheduledIterationAsync(task, loopToken))
                return;
        }
    }

    private async Task RunLongRunningScheduleAsync(TaskInfo task, CancellationToken loopToken)
    {
        while (!loopToken.IsCancellationRequested)
        {
            if (!await ExecuteScheduledIterationAsync(task, loopToken))
                return;

            // 从本轮结束后开始计算间隔，确保释放锁后存在真实的 Pod 交接窗口。
            if (!await TaskExtensions.SafeDelay(task.Interval, loopToken))
                return;
        }
    }

    /// <summary>
    /// 任务循环主方法。
    /// </summary>
    private Task RunTaskLoop(TaskInfo task)
    {
        return Task.Run(async () =>
        {
            try
            {
                var initialDelay = CalculateInitialDelay(task.Name);
                _logger.LogInformation("任务初始延迟: {TaskName}, 延迟: {Delay}", task.Name, initialDelay);

                if (!await TaskExtensions.SafeDelay(initialDelay, _cts.Token))
                {
                    _logger.LogInformation("任务 {TaskName} 在初始延迟期间被取消", task.Name);
                    return;
                }

                using var loopCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, _lifetime.ApplicationStopping);
                var loopToken = loopCts.Token;

                if (task.Type == TaskType.LongRunning)
                    await RunLongRunningScheduleAsync(task, loopToken);
                else
                    await RunRecurringScheduleAsync(task, loopToken);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested || _lifetime.ApplicationStopping.IsCancellationRequested)
            {
                _logger.LogInformation("任务 {TaskName} 循环已停止", task.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "任务 {TaskName} 调度循环异常终止", task.Name);
            }
        }, _cts.Token);
    }

    /// <summary>
    /// 根据任务类型执行
    /// <para>
    /// loopToken 会传入周期任务和长期任务，用于协作式停止。
    /// </para>
    /// </summary>
    private async Task ExecuteByTypeAsync(TaskInfo task, CancellationToken loopToken)
    {
        if (task.Type == TaskType.LongRunning)
        {
            await RunLongRunningWindowAsync(task, loopToken);
            return;
        }

        await task.Action(loopToken);
    }

    private async Task<bool> TryAcquireDedupAsync(TaskInfo task)
    {
        if (_redisClient == null || !task.PreventDuplicateInterval.HasValue) return true;

        var key = $"task:dedup:{task.Name}";
        var acquired = _redisClient.SetNx(key, _podId, task.PreventDuplicateInterval.Value);
        return await Task.FromResult(acquired);
    }

    internal virtual async Task<IAsyncDisposable?> TryAcquireDistributedLockAsync(TaskInfo task)
    {
        if (_redisClient == null) return null;

        var lockKey = $"task:{task.Name}";
        return await Task.FromResult(_redisClient.TryLock(lockKey, task.LockTimeout, enableWatchdog: true));
    }
}
