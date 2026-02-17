# 架构总览

Art Admin 后端采用 **.NET 10 Minimal API** 自创四层清洁架构。

## 分层架构图

```
┌─────────────────────────────────────────────────┐
│                   Art.Api                        │
│           路由入口 · 中间件配置 · 启动            │
│  • Routes (Admin / App / Common)                 │
│  • Program.cs                                    │
│  • TaskConfiguration.cs                          │
└──────────────────┬──────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────┐
│                  Art.Core                        │
│              核心业务逻辑                         │
│  • Services/Admin/  (后台管理)                    │
│  • Services/App/    (客户端业务)                   │
│  • Workers/         (定时任务)                    │
│  • Shared/          (复用逻辑)                    │
└──────────┬───────────────────┬───────────────────┘
           │                   │
┌──────────▼──────────┐  ┌─────▼──────────────────┐
│    Art.Domain       │  │     Art.Infra           │
│    领域层（零依赖）  │  │     基础设施层          │
│  • Entities/        │  │  • Data/ (DbContext)    │
│  • Enums/           │  │  • Cache/ (Redis)       │
│  • Exceptions/      │  │  • Framework/           │
│  • Constants/       │  │  • Logging/             │
│  • IdGen.cs         │  │  • MultiTenancy/        │
└─────────────────────┘  └────────────────────────┘
```

## 中间件管道

请求进入后按以下顺序经过中间件处理：

```
请求 →  CORS
     →  Serilog 请求日志
     →  全局异常处理 (ExceptionMiddleware)
     →  请求/响应日志记录 (RequestResponseLoggingMiddleware)
     →  鉴权 (AuthorizationMiddleware)
     →  Demo 模式限制 (DemoModeMiddleware)
     →  路由匹配 → Service 逻辑执行
```

## 启动配置

`Program.cs` 中的服务注册顺序：

```csharp
// 1. 日志
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.DailyMySQL(connectionString!)
    .CreateLogger();

// 2. 数据库
services.AddDbContextPool<ArtDbContext>((sp, options) =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)))
        .UseSnakeCaseNamingConvention());

// 3. Redis + 雪花 ID
Redis.Initialize(redisConnection);
SnowflakeIdGenerator.Initialize(Redis.Client);

// 4. JSON 宽容配置
services.ConfigureHttpJsonOptions(JsonConfiguration.ConfigureJsonOptions);

// 5. 自动注入服务
services.AutoDependencyInjection();

// 6. 自动注册路由
services.AddApiRouters();

// 7. 任务调度器
services.AddHostedService<TaskScheduler>();
```

## 关键约定

| 约定 | 说明 |
| --- | --- |
| ID 类型 | `long`，雪花 ID 由 `IdGen.NextId()` 生成 |
| 数据库命名 | Snake Case（`created_time`） |
| 服务注入 | `[Service(ServiceLifetime.Scoped)]` |
| 路由接口 | `IAdminRouterBase` / `IAppRouterBase` / `ICommonRouterBase` |
| 列表查询 | 全部使用 POST（请求体传分页 + 筛选参数） |
| DTO 定义 | Service 文件底部 `#region 请求/响应模型` |
| 异常处理 | 直接 throw，中间件统一转 JSON |
| 单行 if | 不加花括号，换行后写语句 |
