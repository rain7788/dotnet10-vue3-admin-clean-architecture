# Snowflake ID

Art Admin uses the **Yitter IdGenerator** to produce unique, time-ordered `long` IDs for all entities.

## Why Not Auto-Increment?

| Feature | Auto-Increment | Snowflake ID |
| --- | --- | --- |
| Distributed | ❌ Requires coordination | ✅ Independent generation |
| Performance | Bottleneck on DB | ✅ In-memory generation |
| Predictability | Sequential, easily guessed | ✅ Non-sequential |
| Migration | Hard to merge data | ✅ Globally unique |

## Usage

```csharp
// Generate a new ID
var id = IdGen.NextId();
// e.g., 1234567890123456789
```

## Configuration

```csharp
// Program.cs — set WorkerId
var options = new IdGeneratorOptions { WorkerId = 1 };
YitIdHelper.SetIdGenerator(options);
```

In Kubernetes, `WorkerId` can be derived from the Pod hostname or environment variable to ensure uniqueness across replicas.

## Frontend Precision

JavaScript `Number.MAX_SAFE_INTEGER` is `2^53 - 1` (9007199254740991). Snowflake IDs are typically 19 digits, exceeding this limit.

Art Admin's `SmartLongConverter` automatically serializes large `long` values as strings:

```json
{ "id": "1234567890123456789" }
```

See [JSON Serialization](/en/backend/json) for details.

## ID Structure

```
| Sign (1 bit) | Timestamp (41 bit) | WorkerId (6 bit) | Sequence (16 bit) |
```

- **Timestamp**: Milliseconds since epoch, supports ~69 years
- **WorkerId**: Supports up to 64 worker nodes
- **Sequence**: Up to 65536 IDs per millisecond per worker
