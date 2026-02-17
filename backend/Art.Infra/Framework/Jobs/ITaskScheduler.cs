namespace Art.Infra.Framework.Jobs;

/// <summary>
/// 任务调度器接口
/// </summary>
public interface ITaskScheduler
{
    /// <summary>
    /// 添加周期性任务
    /// </summary>
    /// <param name="taskAction">任务执行逻辑</param>
    /// <param name="interval">执行间隔（调度器每隔该时间尝试触发一次任务）。</param>
    /// <param name="allowedHours">允许执行的小时范围（可选）。不在允许范围内会跳过执行。</param>
    /// <param name="preventDuplicateInterval">防重复执行间隔（可选）。在该时间窗口内只允许执行一次（基于 Redis 去重 Key）。</param>
    /// <param name="useDistributedLock">
    /// 是否启用分布式锁（默认 true）。
    /// <para>在多 Pod 部署场景，建议保持开启，避免同一任务在多个 Pod 上同时执行。</para>
    /// <para>当 Redis 不可用时，调度器会自动降级为无锁模式（无法跨 Pod 互斥）。</para>
    /// </param>
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
    /// <remarks>
    /// 长期任务采用“两层节奏”：
    /// <list type="bullet">
    /// <item><description><c>interval</c>：外层调度节奏——每隔多久尝试进入一轮“运行窗口”（会参与去重/抢锁）。</description></item>
    /// <item><description><c>runDuration</c>：运行窗口时长——本轮最多运行多久后退出，以便释放分布式锁、让其他 Pod 有机会接管，避免长期独占。</description></item>
    /// <item><description><c>processingInterval</c>：窗口内处理节奏——每次调用 <paramref name="taskAction"/> 之后等待多久再进入下一次处理循环，用于节流和避免空队列时 CPU 空转。</description></item>
    /// </list>
    /// <para>队列消费的常见实践：<paramref name="taskAction"/> 内部每次批量消费 N 条；<c>processingInterval</c> 设为 50-300ms；<c>runDuration</c> 设为 30-120s。</para>
    /// </remarks>
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
