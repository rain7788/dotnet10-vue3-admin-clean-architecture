# Introduction

Art Admin is an **AI-friendly full-stack admin framework** built on **.NET 10 Minimal API** + **Vue 3 (art-design-pro)**.

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
| Multi-Tenancy | `ITenantEntity` + auto QueryFilter |
| Distributed Lock | Redis SetNx + watchdog renewal |
| Message Queue | Redis List (RPUSH / RPOP) |
| Delay Queue | Redis Sorted Set + Lua atomic consume |
| Task Scheduler | Custom scheduler + distributed lock dedup |
| Logging | Serilog daily-partitioned MySQL sink |
| Snowflake ID | Yitter + Redis auto WorkerId allocation |

### Beautiful Frontend

The frontend is based on [art-design-pro](https://www.artd.pro/docs/zh/guide/introduce.html), Vue 3 + Element Plus + TailwindCSS 4.

> For detailed frontend documentation, see the [art-design-pro docs](https://www.artd.pro/docs/zh/)

## Live Demo

- **Admin Panel**: [https://admin.aftbay.com](https://admin.aftbay.com)
- **Swagger API**: [https://api.aftbay.com/swagger](https://api.aftbay.com/swagger)

## License

[MIT License](https://opensource.org/licenses/MIT) — Free for commercial use, no restrictions.
