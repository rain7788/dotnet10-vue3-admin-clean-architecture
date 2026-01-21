namespace SeaCode.Infra.Framework.Jobs;

/// <summary>
/// 任务调度器接口
/// </summary>
public interface ITaskScheduler
{
    /// <summary>
    /// 添加周期性任务
    /// </summary>
    /// <param name="taskAction">任务执行逻辑</param>
    /// <param name="interval">执行间隔</param>
    /// <param name="allowedHours">允许执行的小时范围（可选）</param>
    /// <param name="preventDuplicateInterval">防重复执行间隔</param>
    /// <param name="taskName">任务名称</param>
    void AddRecurringTask(
        Func<CancellationToken, Task> taskAction,
        TimeSpan interval,
        int[]? allowedHours = null,
        TimeSpan? preventDuplicateInterval = null,
        bool useDistributedLock = true,
        string? taskName = null);

    /// <summary>
    /// 添加长期运行任务（消息队列消费等）
    /// </summary>
    void AddLongRunningTask(
        Func<CancellationToken, Task> taskAction,
        TimeSpan interval,
        TimeSpan? processingInterval = null,
        TimeSpan? runDuration = null,
        bool useDistributedLock = true,
        string? taskName = null);

    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

/// <summary>
/// 任务配置接口
/// </summary>
public interface ITaskConfigurationProvider
{
    void ConfigureTasks(ITaskScheduler taskScheduler);
}
