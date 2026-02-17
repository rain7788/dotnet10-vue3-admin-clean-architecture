# Project Structure

## Backend Layered Architecture

```
backend/
├── Art.Api/                    # API entry layer
│   ├── Program.cs              # App startup config
│   ├── Hosting/
│   │   └── TaskConfiguration.cs
│   └── Routes/
│       ├── Admin/              # Admin routes (/admin/*)
│       ├── App/                # App routes (/app/*)
│       └── Common/             # Public routes (/common/*)
│
├── Art.Core/                   # Core business layer
│   ├── Services/
│   │   ├── Admin/              # Admin services (inject RequestContext)
│   │   └── App/                # Client services (inject RequestContext)
│   ├── Workers/                # Background tasks (use IDbContextFactory)
│   └── Shared/                 # Shared logic (no RequestContext)
│
├── Art.Domain/                 # Domain layer (zero dependencies)
│   ├── Entities/
│   ├── Enums/
│   ├── Exceptions/
│   ├── Constants/
│   └── IdGen.cs
│
└── Art.Infra/                  # Infrastructure layer
    ├── Data/                   # DbContext
    ├── Cache/                  # Redis, distributed lock, delay queue
    ├── Framework/              # Core framework
    │   ├── ServiceAttribute.cs
    │   ├── RequestContext.cs
    │   ├── Routes/
    │   ├── Middlewares/
    │   └── Jobs/
    ├── Logging/                # Serilog sinks
    └── MultiTenancy/
```

## Dependency Direction

```
Art.Api  ──────►  Art.Core  ──────►  Art.Domain (zero deps)
                     │
                     ▼
                  Art.Infra
```
