# Automatic Dependency Injection

Art Admin uses the `[Service]` attribute and assembly scanning to automatically register services into the DI container. **No manual registration in Program.cs is needed.**

## Usage

```csharp
[Service(ServiceLifetime.Scoped)]
public class UserService
{
    private readonly ArtDbContext _db;
    private readonly RequestContext _user;

    public UserService(ArtDbContext db, RequestContext user)
    {
        _db = db;
        _user = user;
    }
}
```

Simply add the `[Service]` attribute — the framework scans all assemblies at startup and auto-registers.

## Lifecycle Options

| Lifecycle | Usage |
| --- | --- |
| `Scoped` | One instance per request; **default for most services** |
| `Singleton` | Global singleton, for stateless utilities |
| `Transient` | New instance every injection; **Worker tasks use this** |

## How It Works

At startup, `ServiceCollectionExtensions` scans all classes with `[Service]`:

```csharp
// Framework auto-scans — services don't need manual registration
var types = assemblies
    .SelectMany(a => a.GetTypes())
    .Where(t => t.GetCustomAttribute<ServiceAttribute>() != null);

foreach (var type in types)
{
    var attr = type.GetCustomAttribute<ServiceAttribute>()!;
    services.Add(new ServiceDescriptor(type, type, attr.Lifetime));
}
```

## Important Rules

::: warning
1. **Do NOT register in Program.cs** — adding `[Service]` is sufficient; manual registration causes duplicates
2. **Workers must use `Transient`** — Workers run outside HTTP scope, Scoped lifetime will throw
3. **Workers must NOT inject `RequestContext`** — no user context in background tasks
:::

## RequestContext

`RequestContext` provides the current user's identity info (extracted from Token by middleware):

```csharp
public class RequestContext
{
    public long Id { get; set; }           // User ID
    public string? TenantId { get; set; }  // Tenant ID
    // ...
}
```

Available in `Admin/` and `App/` services only. `Workers/` and `Shared/` must not inject it.
