# Why Not Microservices?

Art Admin uses a **modular monolith architecture** instead of microservices. This page explains the reasoning behind this decision and what to do if you genuinely need microservices.

## Monolith ≠ Mess

Many people misunderstand "monolith" as code piled together without structure. Art Admin's monolith is **modular**:

- Four-layer clean architecture (`Api → Core → Domain ← Infra`), clear responsibilities
- Business modules organized under `Art.Core/Services/`, naturally isolated
- Multi-client API isolation (Admin / App / Common), no interference

## Why Not Microservices?

### 1. Speed First

The framework's goal is to **build applications fast**. Before business explodes, monolith architecture significantly reduces development and operational complexity:

| Dimension | Monolith | Microservices |
| --- | --- | --- |
| Dev & Debug | One process, F5 to start | Multiple services, need orchestration |
| Data Consistency | Local transactions | Distributed transactions (Saga / TCC) |
| Deployment | One container | N containers + service discovery + gateway |
| Team Size | 1-5 people | Usually needs a DevOps team |

### 2. Multi-Client Auth is Built-in

The framework already supports three independent API groups with separate authentication — Admin, App, and Common. No need to split services for multi-client isolation.

### 3. Clean Architecture = Easy Split

The four-layer separation makes future microservice migration low-cost:

```
Art.Core/Services/
├── Admin/
│   ├── UserService.cs          → Split into User microservice
│   ├── OrderService.cs         → Split into Order microservice
│   └── ProductService.cs       → Split into Product microservice
└── App/
    └── AppOrderService.cs      → Merge into Order microservice
```

Split along Domain boundaries, deploy each service independently, share `Domain` and `Infra` layers.

### 4. Avoid Premature Optimization

During the business validation phase, microservices often cause more problems than they solve:

- **Network latency** — inter-service calls add 1-10ms
- **Distributed transactions** — implementing Saga patterns is expensive
- **Operations cost** — service discovery, distributed tracing, log aggregation, config center
- **Debugging difficulty** — issues may span multiple services

> 💡 Premature microservice adoption is one of the most common architectural mistakes. Validate your business with a monolith first, then split when traffic and team size truly demand it.

## If You Actually Need Microservices

When your business scale truly requires microservices, Art Admin's clean architecture enables low-cost migration. Here are the recommended approaches:

### API Gateway Selection

| Gateway | Rating | Description |
| --- | --- | --- |
| **YARP** | ⭐⭐⭐⭐⭐ | Microsoft official, lightweight & high-performance, .NET native |
| **Ocelot** | ⭐⭐⭐⭐ | Mature .NET ecosystem solution, feature-rich, active community |
| Nginx | ⭐⭐⭐ | General purpose, requires extra config maintenance |
| Kong / APISIX | ⭐⭐ | Powerful but introduces additional tech stack |

::: tip Recommended: YARP
For **lightweight and high-performance** needs, choose [YARP](https://github.com/microsoft/reverse-proxy) (Yet Another Reverse Proxy). It's an official Microsoft .NET reverse proxy library that integrates directly into ASP.NET Core — no extra process needed.

```csharp
// YARP gateway configuration example
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

app.MapReverseProxy();
```

```json
{
  "ReverseProxy": {
    "Routes": {
      "user-route": {
        "ClusterId": "user-cluster",
        "Match": { "Path": "/api/user/{**catch-all}" }
      },
      "order-route": {
        "ClusterId": "order-cluster",
        "Match": { "Path": "/api/order/{**catch-all}" }
      }
    },
    "Clusters": {
      "user-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://user-service:8080" }
        }
      },
      "order-cluster": {
        "Destinations": {
          "destination1": { "Address": "http://order-service:8080" }
        }
      }
    }
  }
}
```
:::

If you need **richer features** (rate limiting, circuit breaker, auth out of the box), consider [Ocelot](https://github.com/ThreeMammals/Ocelot):

```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/user/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "user-service", "Port": 8080 }
      ],
      "UpstreamPathTemplate": "/api/user/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ]
    }
  ]
}
```

### Migration Steps

1. **Identify boundaries** — split along module boundaries under `Art.Core/Services/`
2. **Independent databases** — each service owns its own database schema
3. **Add gateway** — use YARP or Ocelot for unified entry point
4. **Async communication** — use message queues for inter-service decoupling (Redis MQ infrastructure already exists)
5. **Migrate gradually** — split one module at a time, not a big-bang rewrite

::: warning Reminder
Microservice migration is a gradual process. Don't try to do it all at once. Start with the highest-traffic or most frequently changed module, keep the rest as monolith.
:::
