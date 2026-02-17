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

        _cache.LPush(CacheKeys.DemoMessageQueue, message);
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
}

public class DemoEnqueueMessageRequest
{
    public string? Message { get; set; }
}

public class DemoQueueStatusResponse
{
    /// <summary>
    /// 队列当前长度（待消费消息数）
    /// </summary>
    public long QueueLength { get; set; }
}
