# Swagger Documentation

Art Admin uses the custom **SwaggerSloop** middleware for API documentation with multi-group support and Token authentication.

## Live Demo

Visit [api.aftbay.com/swagger](https://api.aftbay.com/swagger) to see the live API docs.

## Multi-Group API

APIs are organized into groups displayed separately in Swagger UI:

| Group | Description | Prefix |
| --- | --- | --- |
| `Admin` | Backend management APIs | `/admin/*` |
| `App` | Client APIs | `/app/*` |
| `Common` | Public APIs | `/common/*` |

### Specifying Groups in Routes

```csharp
public class XxxRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var g = group.MapGroup("module/xxx")
            .WithGroupName(ApiGroups.Admin)  // Specify group
            .WithTags("Xxx Management");     // Category tag

        g.MapPost("list", async (XxxListRequest req, XxxService svc)
            => await svc.GetListAsync(req))
            .WithSummary("Query List");      // Endpoint description
    }
}
```

## Token Authentication

Swagger UI integrates Bearer Token authentication. Click the `Authorize` button and enter your token to test protected endpoints.

## SwaggerSloop

[SwaggerSloop](https://github.com/rain7788/SwaggerSloop) is an open-source Swagger enhancement middleware by the Art Admin author:

- Auto-scans Minimal API routes for OpenAPI doc generation
- Multi-group navigation (dropdown to switch Admin / App / Common)
- Built-in Bearer Token auth UI
- Supports `WithSummary()` / `WithDescription()` / `WithTags()`

### Registration

```csharp
// Program.cs
builder.Services.AddSwaggerSloop();
app.UseSwaggerSloop();
```

## Common Annotations

| Method | Purpose |
| --- | --- |
| `.WithGroupName(name)` | Assign API group |
| `.WithTags(tag)` | Category label |
| `.WithSummary(text)` | Brief description |
| `.WithDescription(text)` | Detailed description |
