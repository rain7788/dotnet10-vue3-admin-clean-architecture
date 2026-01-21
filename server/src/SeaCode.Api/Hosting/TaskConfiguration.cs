using SeaCode.Core.Workers;
using SeaCode.Infra.Framework.Jobs;

namespace SeaCode.Api.Hosting;

/// <summary>
/// 后台任务配置
/// </summary>
public class TaskConfiguration : ITaskConfigurationProvider
{
    private readonly DailyWorker _dailyWorker;

    public TaskConfiguration(DailyWorker dailyWorker)
    {
        _dailyWorker = dailyWorker;
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
            preventDuplicateInterval: TimeSpan.FromHours(12),
            useDistributedLock: false);

        // 测试任务：用于验证优雅退出（等待几秒）
        taskScheduler.AddRecurringTask(
            _dailyWorker.TestGracefulShutdown,
            TimeSpan.FromSeconds(15),
            useDistributedLock: false);

        // 可以在此处添加更多任务...
    }
}
