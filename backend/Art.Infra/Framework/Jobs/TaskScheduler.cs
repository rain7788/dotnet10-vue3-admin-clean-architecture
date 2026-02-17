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
[Service(ServiceLifetime.Singleton)]
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

        // 等待所有任务完成，最多等待10秒（给正在执行的任务时间完成业务闭环）
        var allTasks = Task.WhenAll(_runningTasks);
        var completed = await Task.WhenAny(allTasks, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken.None));

        if (completed == allTasks)
        {
            try
            {
                await allTasks;
            }
            catch (OperationCanceledException)
            {
                // 正常取消，视为已停止
            }

            _logger.LogInformation("所有任务已正常停止");
            return;
        }

        _logger.LogWarning("任务停止超时，强制退出");
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
    /// 双层 Token 机制说明：
    /// - loopToken 用于控制循环层（是否继续下一轮定时器等待）
    /// - 传给业务代码的是 CancellationToken.None，确保 HTTP/DB 操作不被中断
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

            // 传入 loopToken 仅用于控制长期任务的循环层
            await ExecuteByTypeAsync(task, loopToken);
            return;
        }

        try
        {
            await ExecuteByTypeAsync(task, loopToken);
        }
        catch (OperationCanceledException)
        {
            // 正常取消（循环层被取消）
            _logger.LogInformation("任务循环被取消: {TaskName}", task.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "任务执行失败: {TaskName}", task.Name);
        }
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
    /// 长期任务循环执行
    /// <para>
    /// 双层 Token 机制：
    /// - loopToken 控制循环是否继续（是否开始新一轮处理、延迟等待）
    /// - 传给 task.Action 的是 CancellationToken.None，确保业务操作完整执行
    /// </para>
    /// </summary>
    private async Task RunLongRunningLoopAsync(TaskInfo task, CancellationToken loopToken)
    {
        var stopAt = DateTime.UtcNow + task.RunDuration;
        var processedCount = 0;

        while (DateTime.UtcNow < stopAt && !loopToken.IsCancellationRequested)
        {
            try
            {
                // 关键：传入 CancellationToken.None，确保 HTTP/DB 操作不被取消
                await task.Action(CancellationToken.None);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "长期任务单次执行失败: {TaskName}", task.Name);
            }

            if (task.ProcessingInterval > TimeSpan.Zero)
            {
                // 延迟使用 loopToken，以便快速响应停止信号
                if (!await TaskExtensions.SafeDelay(task.ProcessingInterval, loopToken))
                {
                    _logger.LogInformation("长期任务 {TaskName} 收到停止信号，等待当前任务完成后退出，共处理 {Count} 次", task.Name, processedCount);
                    return;
                }
            }
        }

        if (loopToken.IsCancellationRequested)
            _logger.LogInformation("长期任务 {TaskName} 循环被取消，共处理 {Count} 次", task.Name, processedCount);
    }

    /// <summary>
    /// 任务循环主方法
    /// <para>
    /// 双层 Token 机制：
    /// - _cts.Token / loopToken 控制定时器循环
    /// - 实际任务执行使用 CancellationToken.None
    /// </para>
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

                var loopToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, _lifetime.ApplicationStopping).Token;

                using var timer = new PeriodicTimer(task.Interval);
                while (await timer.WaitForNextTickAsync(loopToken))
                {
                    await ExecuteTaskSafe(task, loopToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("任务 {TaskName} 循环已停止", task.Name);
            }
        }, _cts.Token);
    }

    /// <summary>
    /// 根据任务类型执行
    /// <para>
    /// 双层 Token 机制：loopToken 仅控制长期任务的循环层，周期任务直接传 CancellationToken.None
    /// </para>
    /// </summary>
    private async Task ExecuteByTypeAsync(TaskInfo task, CancellationToken loopToken)
    {
        if (task.Type == TaskType.LongRunning)
        {
            // 长期任务：loopToken 控制循环层，业务代码使用 CancellationToken.None
            await RunLongRunningLoopAsync(task, loopToken);
            return;
        }

        // 周期任务：传入 CancellationToken.None，确保任务完整执行不被中断
        try
        {
            await task.Action(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "周期任务执行失败: {TaskName}", task.Name);
        }
    }

    private async Task<bool> TryAcquireDedupAsync(TaskInfo task)
    {
        if (_redisClient == null || !task.PreventDuplicateInterval.HasValue) return true;

        var key = $"task:dedup:{task.Name}";
        var acquired = _redisClient.SetNx(key, _podId, task.PreventDuplicateInterval.Value);
        return await Task.FromResult(acquired);
    }

    private async Task<IAsyncDisposable?> TryAcquireDistributedLockAsync(TaskInfo task)
    {
        if (_redisClient == null) return null;

        var lockKey = $"task:{task.Name}";
        return await Task.FromResult(_redisClient.TryLock(lockKey, task.LockTimeout, enableWatchdog: true));
    }
}
