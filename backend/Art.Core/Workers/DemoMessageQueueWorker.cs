using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisClient = FreeRedis.RedisClient;
using Art.Domain.Constants;
using Art.Infra.Framework;

namespace Art.Core.Workers;

/// <summary>
/// Demo：Redis List 消息队列消费 Worker（RPOP）
/// </summary>
[Service(ServiceLifetime.Transient)]
public class DemoMessageQueueWorker
{
    private readonly RedisClient _cache;
    private readonly ILogger<DemoMessageQueueWorker> _logger;

    public DemoMessageQueueWorker(
        RedisClient cache,
        ILogger<DemoMessageQueueWorker> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task ProcessQueue(CancellationToken cancel)
    {
        const int maxBatchSize = 20;
        var processed = 0;

        for (var i = 0; i < maxBatchSize; i++)
        {
            var msg = _cache.RPop(CacheKeys.DemoMessageQueue);
            if (string.IsNullOrEmpty(msg))
            {
                break;
            }

            processed++;
            _logger.LogInformation("[DemoQueue] 消费消息: {Message}", msg);
        }

        if (processed > 0)
        {
            _logger.LogInformation("[DemoQueue] 本轮处理完成: {Count}", processed);
        }

        return Task.CompletedTask;
    }
}
