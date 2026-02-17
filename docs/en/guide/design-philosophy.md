# Design Philosophy

## AI-Friendly

Art Admin's core goal: **let AI efficiently participate in development**.

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

1. **Domain has zero dependencies** — only entities, enums, exceptions
2. **Core is pure business** — no HTTP, no framework concerns
3. **Infra is swappable** — switch DB or cache without affecting business
4. **Api is razor-thin** — only route mapping, one line per Service method

## Pragmatic Choices

- **No JWT** — Reference Token supports instant revocation
- **No MediatR** — direct Service injection, fewer layers
- **No FluentValidation** — validate in Service methods, more readable
- **IDs are `long`** — Snowflake ID + `SmartLongConverter` for JS safety
