# Dynamic Query

Art Admin uses **LinqKit** `PredicateBuilder` for building dynamic query conditions, avoiding raw SQL concatenation.

## Basic Pattern

```csharp
public async Task<object> GetListAsync(UserListRequest req)
{
    var predicate = PredicateBuilder.New<SysUser>(true);

    if (!string.IsNullOrWhiteSpace(req.Keyword))
        predicate = predicate.And(x =>
            x.Username.Contains(req.Keyword) ||
            x.Nickname!.Contains(req.Keyword));

    if (req.Status.HasValue)
        predicate = predicate.And(x => x.Status == req.Status.Value);

    if (req.RoleId.HasValue)
        predicate = predicate.And(x => x.RoleId == req.RoleId.Value);

    var query = _db.SysUsers.AsExpandable().Where(predicate);

    var total = await query.CountAsync();
    var items = await query
        .OrderByDescending(x => x.Id)
        .Skip(((req.PageIndex ?? 1) - 1) * (req.PageSize ?? 20))
        .Take(req.PageSize ?? 20)
        .ToListAsync();

    return new { total, items };
}
```

## Key Points

### PredicateBuilder.New\<T\>(true)

The `true` parameter creates a default "match all" predicate. Subsequent `.And()` calls narrow results:

```csharp
// Start with "all records"
var predicate = PredicateBuilder.New<SysUser>(true);

// Add conditions only when filter values exist
if (!string.IsNullOrWhiteSpace(req.Keyword))
    predicate = predicate.And(x => x.Username.Contains(req.Keyword));
```

### AsExpandable()

::: warning Required
`AsExpandable()` must be called before `.Where(predicate)`. This is a LinqKit requirement — without it, EF Core cannot translate the expression tree properly.
:::

```csharp
var query = _db.SysUsers
    .AsExpandable()         // Required by LinqKit
    .Where(predicate);
```

### Date Range Queries

```csharp
if (req.StartDate.HasValue)
    predicate = predicate.And(x => x.CreatedTime >= req.StartDate.Value);

if (req.EndDate.HasValue)
    predicate = predicate.And(x => x.CreatedTime <= req.EndDate.Value.AddDays(1));
```

### OR Conditions

```csharp
// Search across multiple fields
predicate = predicate.And(x =>
    x.Username.Contains(keyword) ||
    x.Nickname!.Contains(keyword) ||
    x.Email!.Contains(keyword));
```

## Pagination DTO Convention

```csharp
// Request
public class UserListRequest
{
    public int? PageIndex { get; set; } = 1;
    public int? PageSize { get; set; } = 20;
    public string? Keyword { get; set; }
    public int? Status { get; set; }
}

// Response
return new { total, items };
```

The frontend `useTable` composable automatically recognizes `total` + `items` fields.
