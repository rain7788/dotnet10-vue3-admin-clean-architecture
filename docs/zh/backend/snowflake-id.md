# 雪花 ID

Art Admin 使用 **Yitter** 雪花 ID 算法生成全局唯一的 `long` 型 ID，支持 Redis 自动分配 WorkerId。

## 使用

```csharp
// Domain 层封装 — 任何地方都可以调用
var id = IdGen.NextId();
```

实体基类已内置（无需手动调用）：

```csharp
public abstract class EntityBase
{
    [Key]
    public long Id { get; set; } = IdGen.NextId(); // 自动生成
}
```

## 配置参数

| 参数 | 值 | 说明 |
| --- | --- | --- |
| WorkerIdBitLength | 6 | 最大 64 个 Worker（0-63） |
| SeqBitLength | 10 | 每毫秒 1024 个 ID |
| 吞吐量 | ~100 万/秒 | 远超大多数业务场景 |

## 初始化

### Redis 自动分配 WorkerId（推荐）

```csharp
// Program.cs
Redis.Initialize(redisConnection);
SnowflakeIdGenerator.Initialize(Redis.Client);
```

通过 Redis `INCR` 原子递增 + 取模，自动分配唯一 WorkerId：

```csharp
private static ushort AllocateWorkerId(RedisClient client)
{
    const string redisKey = "art:snowflake:workerid";
    var counter = client.IncrBy(redisKey, 1);
    return (ushort)(counter % (MaxWorkerId + 1)); // 0-63 循环分配
}
```

### 手动指定 WorkerId

```csharp
// 无 Redis 时，通过环境变量指定
var workerId = ushort.Parse(Environment.GetEnvironmentVariable("SNOWFLAKE_WORKER_ID") ?? "1");
SnowflakeIdGenerator.Initialize(workerId);
```

## 前端精度处理

JavaScript `Number.MAX_SAFE_INTEGER = 2^53 - 1`，雪花 ID 可能超出安全范围。

框架通过 `SmartLongConverter` 自动将所有 `long` 类型序列化为字符串：

```csharp
public class SmartLongConverter : JsonConverter<long>
{
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        // 所有 long 统一转为字符串，避免前端精度丢失
        writer.WriteStringValue(value.ToString());
    }
}
```

前端拿到的 ID 始终是字符串 `"1234567890123456789"`，不会有精度问题。
