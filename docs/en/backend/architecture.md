# Architecture Overview

Art Admin backend adopts a **4-layer architecture** with clear responsibilities and dependency direction.

## Layer Diagram

```
Art.Api (Route entry, no business logic)
    ↓
Art.Core (Business logic)
    ↓           ↘
Art.Domain       Art.Infra
(Entities/Enums   (DbContext/Cache
 zero dependency)  framework support)
```

## Layer Responsibilities

| Layer | Project | Responsibilities |
| --- | --- | --- |
| **API** | `Art.Api` | Route registration, middleware pipeline, startup config |
| **Core** | `Art.Core` | Business services, DTO definitions, Worker tasks |
| **Domain** | `Art.Domain` | Entities, enums, exceptions, constants (zero dependencies) |
| **Infra** | `Art.Infra` | EF Core DbContext, Redis cache, logging, framework support |

## Directory Structure

```
Art.Api/
├── Routes/
│   ├── Admin/          # Backend management routes (/admin/*)
│   ├── App/            # Client routes (/app/*)
│   └── Common/         # Public routes (/common/*)
├── Hosting/
│   └── TaskConfiguration.cs
└── Program.cs

Art.Core/
├── Services/
│   ├── Admin/          # Backend management services
│   ├── App/            # Client services
├── Shared/             # Shared logic (no RequestContext)
└── Workers/            # Scheduled tasks

Art.Domain/
├── Entities/           # EF Core entities
├── Enums/              # Enum definitions
├── Exceptions/         # Custom exceptions
├── Constants/          # Constants
└── Models/             # Cross-service shared DTOs

Art.Infra/
├── Data/               # DbContext
├── Cache/              # Redis wrapper
├── Framework/          # Auto DI, Task Scheduler, etc.
├── Logging/            # Daily-partitioned log Sink
└── Common/             # JSON config, utilities
```

## Dependency Rules

- `Art.Domain` depends on nothing — pure entities and enums
- `Art.Infra` depends on `Art.Domain` — implements data access
- `Art.Core` depends on `Art.Domain` + `Art.Infra` — business implementation
- `Art.Api` depends on `Art.Core` — only for route registration, contains no business logic

## Service Categorization

| Directory | Purpose | RequestContext |
| --- | --- | --- |
| `Core/Services/Admin/` | Backend management | ✅ `_user.Id` |
| `Core/Services/App/` | Client business | ✅ `_user.Id` |
| `Core/Workers/` | Scheduled tasks | ❌ Use `IDbContextFactory` |
| `Core/Shared/` | Shared logic (params passed in) | ❌ |
