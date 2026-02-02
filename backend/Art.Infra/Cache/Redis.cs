using System.Text.Json;
using FreeRedis;

namespace Art.Infra.Cache;

/// <summary>
/// Redis 静态客户端
/// </summary>
public static class Redis
{
    /// <summary>
    /// Redis 客户端实例
    /// </summary>
    public static RedisClient Client { get; private set; } = default!;

    /// <summary>
    /// 是否已初始化
    /// </summary>
    public static bool IsInitialized => Client != null;

    /// <summary>
    /// 初始化 Redis 客户端
    /// </summary>
    public static void Initialize(string connectionString, JsonSerializerOptions? jsonOptions = null)
    {
        if (Client != null) return;

        Client = new RedisClient(connectionString);

        var options = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        Client.Serialize = obj => JsonSerializer.Serialize(obj, options);
        Client.Deserialize = (json, type) => JsonSerializer.Deserialize(json, type, options);
    }

    /// <summary>
    /// 释放连接
    /// </summary>
    public static void Dispose()
    {
        Client?.Dispose();
        Client = null!;
    }
}
