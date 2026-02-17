# Redis Cache

Art Admin uses **FreeRedis** as the Redis client, providing cache read/write, pub/sub, distributed lock, message queue, and delay queue capabilities.

## Configuration

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Redis": "127.0.0.1:6379,password=,defaultDatabase=0"
  }
}
```

## Basic Operations

```csharp
[Service(ServiceLifetime.Scoped)]
public class CacheService
{
    private readonly RedisClient _redis;

    public CacheService(RedisClient redis) => _redis = redis;

    // String operations
    public async Task SetAsync(string key, string value, int expireSeconds = 3600)
    {
        await _redis.SetAsync(key, value, expireSeconds);
    }

    public async Task<string?> GetAsync(string key)
    {
        return await _redis.GetAsync(key);
    }

    // JSON serialization
    public async Task SetObjectAsync<T>(string key, T obj, int expireSeconds = 3600)
    {
        var json = JsonSerializer.Serialize(obj);
        await _redis.SetAsync(key, json, expireSeconds);
    }

    public async Task<T?> GetObjectAsync<T>(string key)
    {
        var json = await _redis.GetAsync(key);
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }
}
```

## Cache Key Management

Centralized cache key constants in `CacheKeys.cs`:

```csharp
public static class CacheKeys
{
    public const string UserInfo = "user:info:";
    public const string RolePermissions = "role:permissions:";
    public const string TenantConfig = "tenant:config:";
}

// Usage
var key = CacheKeys.UserInfo + userId;
await _redis.SetAsync(key, json, 3600);
```

## Extended Features

Redis serves as the backbone for several framework features:

- [Distributed Lock](/en/backend/distributed-lock) — Mutex for shared resources
- [Message Queue](/en/backend/message-queue) — Redis List-based async messaging
- [Delay Queue](/en/backend/delay-queue) — Redis Sorted Set-based delayed tasks
