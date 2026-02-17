using Art.Core.Workers;
using Art.Infra.Framework.Jobs;

namespace Art.Api.Hosting;

/// <summary>
/// 后台任务配置
/// </summary>
public class TaskConfiguration : ITaskConfigurationProvider
{
    private readonly DailyWorker _dailyWorker;
    private readonly DemoMessageQueueWorker _demoMessageQueueWorker;

    public TaskConfiguration(
        DailyWorker dailyWorker,
        DemoMessageQueueWorker demoMessageQueueWorker)
    {
        _dailyWorker = dailyWorker;
        _demoMessageQueueWorker = demoMessageQueueWorker;
    }

    /// <summary>
    /// 配置后台任务
    /// </summary>
    public void ConfigureTasks(ITaskScheduler taskScheduler)
    {
        // 配置每日日志清理任务
        // - 每 21 分钟检查一次
        // - 只在凌晨 2-3 点执行
        // - 12 小时内不重复执行
        taskScheduler.AddRecurringTask(
            _dailyWorker.ClearLogs,
            TimeSpan.FromMinutes(21),
            allowedHours: [2, 3],
            preventDuplicateInterval: TimeSpan.FromHours(12));

        // 测试任务：用于验证优雅退出（等待几秒）
        taskScheduler.AddRecurringTask(
            _dailyWorker.TestGracefulShutdown,
            TimeSpan.FromSeconds(15));

        // Demo：Redis List 消息队列消费（RPOP）
        // 参数说明：
        // - interval：外层调度间隔（多久尝试进入一轮运行窗口/抢锁）
        // - runDuration：单轮运行窗口时长（到点退出，释放锁，避免长期独占）
        // - processingInterval：窗口内每次处理后的延迟（避免空队列 CPU 空转 + 控制节奏）
        taskScheduler.AddLongRunningTask(
            _demoMessageQueueWorker.ProcessQueue,
            interval: TimeSpan.FromSeconds(1),
            processingInterval: TimeSpan.FromMilliseconds(20),
            runDuration: TimeSpan.FromSeconds(30),
            taskName: "demo.queue.consume");

        // 可以在此处添加更多任务...
    }
}
