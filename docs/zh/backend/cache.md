# Redis 缓存

Art Admin 使用 **FreeRedis** 作为 Redis 客户端，提供静态封装便于全局使用。

## 初始化

```csharp
// Program.cs
var redisConnection = configuration.GetConnectionString("Redis");
Redis.Initialize(redisConnection);
services.AddSingleton(_ => Redis.Client);
```

## 基本操作

```csharp
[Service(ServiceLifetime.Scoped)]
public class XxxService
{
    private readonly RedisClient _cache;

    public XxxService(RedisClient cache)
    {
        _cache = cache;
    }

    public void Example()
    {
        // 字符串
        _cache.Set("key", "value", 3600); // 过期时间（秒）
        var value = _cache.Get("key");

        // 泛型
        _cache.Set("user:1", new { Name = "Tom" }, 600);
        var user = _cache.Get<UserInfo>("user:1");

        // 判断存在
        var exists = _cache.Exists("key");

        // 删除
        _cache.Del("key");

        // 原子递增
        var count = _cache.IncrBy("counter", 1);

        // Hash
        _cache.HSet("hash", "field", "value");
        var field = _cache.HGet("hash", "field");

        // List
        _cache.LPush("queue", "message");
        var msg = _cache.RPop("queue");
    }
}
```

## 缓存 Key 管理

集中定义在 `Art.Domain/Constants/CacheKeys.cs`：

```csharp
public static class CacheKeys
{
    public static string UserToken(string tokenHash) => $"token:{tokenHash}";
    public static string LoginFailCount(string key) => $"login:fail:{key}";

    public const string DemoMessageQueue = "demo:queue:message";
    public const string DemoDelayQueue = "demo:queue:delay";
}
```

## 生命周期管理

应用停止时自动清理连接：

```csharp
app.Lifetime.ApplicationStopped.Register(() =>
{
    Redis.Dispose();
});
```
