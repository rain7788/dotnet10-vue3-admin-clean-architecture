using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Art.Infra.Data;
using Art.Infra.Framework;
using Art.Infra.Logging;

namespace Art.Core.Workers;

/// <summary>
/// 每日任务 Worker
/// </summary>
[Service(ServiceLifetime.Transient)]
public class DailyWorker
{
    private readonly IDbContextFactory<ArtDbContext> _contextFactory;
    private readonly ILogger<DailyWorker> _logger;

    public DailyWorker(
        IDbContextFactory<ArtDbContext> contextFactory,
        ILogger<DailyWorker> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <summary>
    /// 清理日志，删除过期的日志分表
    /// </summary>
    public async Task ClearLogs(CancellationToken cancel)
    {
        try
        {
            using var context = _contextFactory.CreateDbContext();
            var connectionString = context.Database.GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("清理日志失败：无法获取数据库连接字符串");
                return;
            }

            var tableManager = new DailyLogTableManager(connectionString);

            // 删除 30 天前的日志表
            await tableManager.DropOldTablesAsync(30);

            _logger.LogInformation("日志清理完成，已删除 30 天前的日志表");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理日志失败");
        }
    }

    /// <summary>
    /// 测试任务：用于验证优雅退出
    /// </summary>
    public async Task TestGracefulShutdown(CancellationToken cancel)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancel);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("测试任务被取消（优雅退出）");
        }
    }
}
