using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisClient = FreeRedis.RedisClient;
using Art.Domain.Constants;
using Art.Infra.Cache;
using Art.Infra.Framework;

namespace Art.Core.Workers;

/// <summary>
/// Demo：Redis 延迟队列消费 Worker（Sorted Set ZRANGEBYSCORE + ZREM）
/// </summary>
[Service(ServiceLifetime.Transient)]
public class DemoDelayQueueWorker
{
    private readonly RedisClient _cache;
    private readonly ILogger<DemoDelayQueueWorker> _logger;

    public DemoDelayQueueWorker(
        RedisClient cache,
        ILogger<DemoDelayQueueWorker> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task ProcessQueue(CancellationToken cancel)
    {
        var messages = _cache.DelayQueueConsume(CacheKeys.DemoDelayQueue, maxCount: 20);

        if (messages.Length == 0) return Task.CompletedTask;

        foreach (var msg in messages)
        {
            _logger.LogInformation("[DemoDelayQueue] 消费到期消息: {Message}", msg);
        }

        _logger.LogInformation("[DemoDelayQueue] 本轮处理完成: {Count} 条", messages.Length);

        return Task.CompletedTask;
    }
}
