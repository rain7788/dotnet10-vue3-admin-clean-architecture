# Design Philosophy

## AI-Friendly

Art Admin's core goal: **let AI efficiently participate in development**.

Traditional framework problems:
- Inconsistent code structure, AI needs extensive context to understand
- Many implicit conventions (convention-based DI, magic strings), AI makes mistakes easily
- Complex framework APIs, AI-generated code often doesn't meet framework requirements

Art Admin's solutions:
- **Consistent code patterns** — all Services use `[Service]`, all Routers implement `IAdminRouterBase`
- **Explicit declarations** — `[Service(ServiceLifetime.Scoped)]` clearly states the lifecycle
- **Copilot instructions** — `.github/copilot-instructions.md` with complete coding guidelines
- **Simple APIs** — Minimal API routing, `PredicateBuilder` queries, `throw new BadRequestException()`

## Convention over Configuration

| Convention | Description |
| --- | --- |
| `[Service]` | Auto-scan and register, no manual DI setup |
| `IAdminRouterBase` | Auto-register to `/admin/*` route group |
| `EntityBase` | Built-in Snowflake ID + CreatedTime |
| `PredicateBuilder` | Unified dynamic query builder |
| Exceptions as responses | throw → middleware auto-converts to JSON |
| Co-located DTOs | Request/Response models at bottom of Service file |

## Clean Architecture

```
Art.Api (route entry, no business logic)
  → Art.Core (business logic)
    → Art.Domain (entities/enums/exceptions, zero dependencies)
  ↘ Art.Infra (DbContext/cache/framework support)
```

1. **Domain has zero dependencies** — only entities, enums, exceptions
2. **Core is pure business** — no HTTP, no framework concerns
3. **Infra is swappable** — switch DB or cache without affecting business
4. **Api is razor-thin** — only route mapping, one line per Service method

## Pragmatic Choices

- **No JWT** — Reference Token supports instant revocation
- **No MediatR** — direct Service injection, fewer layers
- **No FluentValidation** — validate in Service methods, more readable
- **IDs are `long`** — Snowflake ID + `SmartLongConverter` for JS safety
- **Single-line `if` without braces** — reduce line count, improve readability

## Modular Monolith

Art Admin uses a **modular monolith** architecture instead of microservices. See [Why Not Microservices](/en/guide/why-not-microservices) for details.

Key considerations:

- **Speed first** — before business explodes, monolith architecture significantly reduces development and operational complexity
- **Clean architecture = easy split** — four-layer separation makes future microservice migration low-cost (split along Domain boundaries)
- **Avoid premature optimization** — microservice overhead (network latency, distributed transactions, DevOps) isn't worth it during validation

## Graceful Shutdown

The framework implements a complete graceful shutdown mechanism to ensure no running tasks are lost:

1. **Receive stop signal** — Docker `SIGTERM` or `Ctrl+C`
2. **Cancel task loops** — notify all background tasks to stop
3. **Wait for completion** — give running tasks 10 seconds to finish current work
4. **Release resources** — close Redis connections, flush log buffers

```csharp
// Resource cleanup in Program.cs
app.Lifetime.ApplicationStopped.Register(() =>
{
    Redis.Dispose();      // Close Redis connection
    Log.CloseAndFlush();  // Flush logs to database
});
```

> Docker's default 10-second grace period aligns with the framework's wait time. If tasks may exceed 10 seconds, increase `stop_grace_period: 30s` in `docker-compose.yml`.
