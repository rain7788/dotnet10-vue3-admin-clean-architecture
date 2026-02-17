# Introduction

Art Admin is an **AI-friendly full-stack admin framework** built on **.NET 10 Minimal API** + **Vue 3 (art-design-pro)**. It features a clean architecture design with complete permission management, user authentication, and multi-client API routing.

## Why Art Admin?

### Designed for AI Collaboration

Traditional admin frameworks have redundant code, inconsistent styles, and many implicit conventions — making it hard for AI to understand and generate correct code. Art Admin solves this at the architecture level:

- **Clean Layering** — Four-layer architecture with clear responsibilities; AI knows where code belongs
- **Convention over Configuration** — `[Service]` for auto-DI, `IAdminRouterBase` for auto-routing, zero boilerplate
- **Consistent Code Patterns** — All Services, Routers, and Entities follow the same conventions
- **Built-in Copilot Instructions** — `.github/copilot-instructions.md` with complete coding guidelines

### Batteries Included

| Feature | Implementation |
| --- | --- |
| Auto DI | `[Service]` attribute + assembly scanning |
| Auto Routing | `IAdminRouterBase` / `IAppRouterBase` / `ICommonRouterBase` |
| Authentication | Reference Token + Redis cache |
| RBAC Permissions | User → Role → Permission → Menu complete access control |
| Multi-Tenancy | `ITenantEntity` + auto QueryFilter |
| Distributed Lock | Redis SetNx + watchdog renewal |
| Message Queue | Redis List (RPUSH / RPOP) |
| Delay Queue | Redis Sorted Set + Lua atomic consume |
| Task Scheduler | Custom scheduler + distributed lock dedup + graceful shutdown |
| Logging | Serilog daily-partitioned MySQL sink |
| Snowflake ID | Yitter + Redis auto WorkerId allocation |

### Beautiful Frontend

The frontend is based on [art-design-pro](https://www.artd.pro/docs/zh/guide/introduce.html), Vue 3 + Element Plus + TailwindCSS 4.

> For detailed frontend documentation, see the [art-design-pro docs](https://www.artd.pro/docs/zh/)

### Multi-Client API Support

The framework provides three independent API groups that can be freely extended:

| Interface | Prefix | Auth |
| --- | --- | --- |
| `IAdminRouterBase` | `/admin/*` | Platform Token |
| `IAppRouterBase` | `/app/*` | Client Token |
| `ICommonRouterBase` | `/common/*` | Public |

## Tech Stack

| Technology | Version | Description |
| --- | --- | --- |
| .NET | 10.0 | Runtime framework |
| ASP.NET Core | 10.0 | Web framework (Minimal API) |
| Entity Framework Core | 9.0 | ORM |
| Pomelo.EntityFrameworkCore.MySql | 9.0 | MySQL driver |
| FreeRedis | 1.5.5 | Redis client |
| Serilog | 4.3.0 | Structured logging |
| Yitter.IdGenerator | 1.0.14 | Snowflake ID generator |
| Swashbuckle | 10.1.0 | Swagger docs |
| Flurl.Http | 4.0.2 | HTTP client |
| Vue 3 | 3.x | Frontend framework (Composition API) |
| Element Plus | - | UI components |
| TailwindCSS | 4.x | Utility-first CSS |
| Vite | 7.x | Lightning build |
| Pinia | - | State management |

## Live Demo

- **Admin Panel**: [https://admin.aftbay.com](https://admin.aftbay.com)
- **Swagger API**: [https://api.aftbay.com/swagger](https://api.aftbay.com/swagger)

> Demo account: `admin` / `123456`
>
> The demo environment has **Demo Mode** enabled — all write operations (create, edit, delete, change password) are intercepted and won't persist to the database.

### Demo Mode

The project has a built-in demo mode switch for deploying public demo sites. When enabled, users can browse and log in normally, but all write operations return a `403 Demo environment, data modification not allowed` response.

**Enable/disable** in `backend/Art.Api/appsettings.json`:

```json
{
  "Settings": {
    "DemoMode": true
  }
}
```

Or via environment variable: `Settings__DemoMode=true`

> 💡 **Remove demo mode entirely**: Delete `DemoModeMiddleware.cs` and remove `app.UseMiddleware<DemoModeMiddleware>();` from `Program.cs`.

## License

[MIT License](https://opensource.org/licenses/MIT) — Free for commercial use, no restrictions.
