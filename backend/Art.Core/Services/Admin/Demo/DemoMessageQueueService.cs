using Microsoft.Extensions.DependencyInjection;
using RedisClient = FreeRedis.RedisClient;
using Art.Domain.Constants;
using Art.Domain.Exceptions;
using Art.Infra.Framework;

namespace Art.Core.Services.Admin.Demo;

/// <summary>
/// Demo：Redis List 消息队列（LPUSH + RPOP）
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class DemoMessageQueueService
{
    private readonly RedisClient _cache;

    public DemoMessageQueueService(RedisClient cache)
    {
        _cache = cache;
    }

    public Task EnqueueAsync(DemoEnqueueMessageRequest request)
    {
        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new BadRequestException("消息内容不能为空");
        }

        CheckRateLimit();
        _cache.LPush(CacheKeys.DemoMessageQueue, message);
        return Task.CompletedTask;
    }

    public Task BatchEnqueueAsync(DemoBatchEnqueueMessageRequest request)
    {
        if (request.Messages == null || request.Messages.Count == 0)
        {
            throw new BadRequestException("消息列表不能为空");
        }

        if (request.Messages.Count > 100)
        {
            throw new BadRequestException("单次最多发送 100 条消息");
        }

        var validMessages = request.Messages
            .Select(m => m?.Trim())
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToArray();

        if (validMessages.Length == 0)
        {
            throw new BadRequestException("消息内容不能全部为空");
        }

        CheckRateLimit();

        foreach (var msg in validMessages)
        {
            _cache.LPush(CacheKeys.DemoMessageQueue, msg!);
        }

        return Task.CompletedTask;
    }

    public Task<DemoQueueStatusResponse> GetQueueStatusAsync()
    {
        var length = _cache.LLen(CacheKeys.DemoMessageQueue);
        return Task.FromResult(new DemoQueueStatusResponse
        {
            QueueLength = length
        });
    }

    /// <summary>
    /// Redis 限流：全局每 5 秒最多调用 3 次入队操作
    /// </summary>
    private void CheckRateLimit()
    {
        var key = CacheKeys.DemoQueueRateLimit;
        var count = _cache.Incr(key);
        if (count == 1)
        {
            _cache.Expire(key, 5); // 5 秒窗口
        }

        if (count > 3)
        {
            throw new BadRequestException("操作太频繁，请稍后再试");
        }
    }
}

public class DemoEnqueueMessageRequest
{
    public string? Message { get; set; }
}

public class DemoBatchEnqueueMessageRequest
{
    public List<string> Messages { get; set; } = [];
}

public class DemoQueueStatusResponse
{
    /// <summary>
    /// 队列当前长度（待消费消息数）
    /// </summary>
    public long QueueLength { get; set; }
}
