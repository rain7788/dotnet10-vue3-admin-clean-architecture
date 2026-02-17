# Optimistic Concurrency Control

Art Admin uses EF Core's `[ConcurrencyCheck]` attribute for optimistic concurrency control — a **safety net** that prevents silent data overwrites when multiple admins edit the same record simultaneously.

## The Problem

```
Admin A reads config: value = "old"
Admin B reads config: value = "old"
Admin A updates: value = "foo" ✅
Admin B updates: value = "bar" ← overwrites A's change silently?
```

::: warning Important
Optimistic locking is a **safety net** mechanism — it throws an exception on conflict, requiring the user to retry. It is only suitable for **low-contention scenarios** (e.g., admin editing configurations). For **high-concurrency writes** (inventory, flash sales, balance changes), use a [distributed lock](/en/backend/distributed-lock) instead.
:::

## Add Concurrency Field

Reuse the `UpdatedTime` field as the concurrency token — no extra column needed:

```csharp
[Table("sys_config")]
public class SysConfig : EntityBaseWithUpdate
{
    [MaxLength(100)]
    public string ConfigKey { get; set; } = default!;

    [MaxLength(500)]
    public string ConfigValue { get; set; } = default!;

    [MaxLength(200)]
    public string? Remark { get; set; }

    /// <summary>
    /// Use UpdatedTime as optimistic lock field.
    /// If two admins edit simultaneously, the later save gets a conflict error.
    /// </summary>
    [ConcurrencyCheck]
    public new DateTime? UpdatedTime { get; set; }
}
```

## How It Works

EF Core adds `UpdatedTime` to the `WHERE` clause:

```sql
-- Admin A reads UpdatedTime = '2025-03-15 10:00:00'
UPDATE sys_config
SET config_value = 'foo', updated_time = NOW()
WHERE id = 1 AND updated_time = '2025-03-15 10:00:00';
-- affected 1 row ✅

-- Admin B still has UpdatedTime = '2025-03-15 10:00:00'
UPDATE sys_config
SET config_value = 'bar', updated_time = NOW()
WHERE id = 1 AND updated_time = '2025-03-15 10:00:00';
-- affected 0 rows → DbUpdateConcurrencyException ❌
```

## Exception Handling

```csharp
public async Task UpdateConfigAsync(ConfigUpdateRequest req)
{
    var entity = await _db.SysConfig.FindAsync(req.Id)
        ?? throw new NotFoundException("Config not found");

    entity.ConfigValue = req.ConfigValue;
    entity.Remark = req.Remark;
    entity.UpdatedTime = DateTime.Now;

    try
    {
        await _db.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        // Safety net: another admin just modified this record
        throw new BadRequestException("This record has been modified by another user, please refresh and try again");
    }
}
```

## Why Use UpdatedTime?

- Reuses the existing `UpdatedTime` field from `EntityBaseWithUpdate`
- No additional column needed
- For admin scenarios, second-level precision is sufficient

## When to Use

| Scenario | Recommended Approach | Why |
| --- | --- | --- |
| System config editing | ✅ Optimistic lock | Low contention, conflict prompt is fine |
| Role/permission editing | ✅ Optimistic lock | Low-frequency admin operations |
| User profile updates | ❌ Not needed | Self-editing only, overwrite is fine |
| Inventory / flash sales | ❌ **Distributed lock** | High concurrency, optimistic lock causes excessive exceptions |
| Balance changes | ❌ **Distributed lock** | Requires strict mutual exclusion |
| Order status changes | ❌ **Distributed lock** | High contention, retry UX is poor |
| Log/record inserts | ❌ Not needed | Append-only, no conflicts |

## Optimistic Lock vs Distributed Lock

| | Optimistic Lock `[ConcurrencyCheck]` | Distributed Lock `RedisLocker` |
| --- | --- | --- |
| Role | Safety net (last line of defense) | Active mutual exclusion |
| Implementation | Database WHERE clause | Redis SetNx |
| On conflict | Throws exception, user retries manually | Waits for lock or fails immediately |
| Performance | High (no extra IO) | Requires Redis round-trip |
| Best for | Low contention (admin config editing) | High contention (inventory, balance, flash sales) |

::: tip Best Practice
- **Admin management** (editing configs, roles, menus): add `[ConcurrencyCheck]` as a safety net
- **High-concurrency writes** (inventory, balance, flash sales): use [distributed lock](/en/backend/distributed-lock) — optimistic lock is not suitable
- The two are complementary: distributed lock for primary protection, optimistic lock as the final safety net
:::
