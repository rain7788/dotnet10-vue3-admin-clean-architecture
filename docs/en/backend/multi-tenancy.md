# Multi-Tenancy

Art Admin implements **row-level multi-tenancy** with automatic query filtering using EF Core global query filters.

## Architecture

```
Request → Token Middleware → Extract TenantId → RequestContext.TenantId
                                                        ↓
                                          EF Core Global Query Filter
                                          WHERE tenant_id = 'xxx'
```

## Entity Interface

Entities requiring tenant isolation implement `ITenantEntity`:

```csharp
public interface ITenantEntity
{
    string? TenantId { get; set; }
}

[Table("biz_order")]
public class BizOrder : EntityBase, ITenantEntity
{
    public string? TenantId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    // ...
}
```

## Automatic Filtering

In `ArtDbContext`, global query filters are applied to all `ITenantEntity` entities:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
        {
            // Add WHERE tenant_id = @currentTenantId
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(/* tenant filter expression */);
        }
    }
}
```

### Effect

```csharp
// What you write
var orders = await _db.BizOrders.ToListAsync();

// What EF Core actually executes
SELECT * FROM biz_order WHERE tenant_id = 'tenant_001';
```

**Every query is automatically filtered** — developers don't need to add `WHERE tenant_id = ...` manually.

## Auto-Fill on Save

When creating new entities, `TenantId` is automatically filled from `RequestContext`:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
    {
        if (entry.State == EntityState.Added)
            entry.Entity.TenantId = _currentTenantId;
    }
    return await base.SaveChangesAsync(cancellationToken);
}
```

## Bypass Filtering

For cross-tenant operations (e.g., platform admin):

```csharp
// Ignore global filters
var allOrders = await _db.BizOrders
    .IgnoreQueryFilters()
    .ToListAsync();
```

## Tenant Identification

Tenants are identified through a field in the user Token, stored in `RequestContext.TenantId`. Each request is guaranteed to only access its own tenant data.
