# 自动依赖注入

Art Admin 使用 `[Service]` 特性标记需要自动注入的服务类，启动时自动扫描所有 `Art.*` 程序集中的标记类并注册到 DI 容器。

## 基本用法

```csharp
[Service(ServiceLifetime.Scoped)]
public class SysUserService
{
    private readonly ArtDbContext _db;
    private readonly RequestContext _user;

    public SysUserService(ArtDbContext db, RequestContext user)
    {
        _db = db;
        _user = user;
    }
}
```

## 生命周期

| 生命周期 | 说明 | 典型场景 |
| --- | --- | --- |
| `Scoped` | 每个请求一个实例（默认） | Service、Repository |
| `Transient` | 每次注入创建新实例 | Worker、无状态工具 |
| `Singleton` | 全局单例 | 配置、TaskScheduler |

## ServiceAttribute 源码

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute : Attribute
{
    public ServiceLifetime LifeTime { get; }

    public ServiceAttribute(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        LifeTime = serviceLifetime;
    }
}
```

## 扫描注册逻辑

`AutoDependencyInjection()` 方法自动扫描所有 `Art.*` 程序集：

1. 获取所有 `Art.*` 开头的已加载程序集和引用程序集
2. 查找所有标注了 `[Service]` 的**非抽象类**
3. 如果类实现了接口（排除 `System.*`），按**接口类型**注册
4. 否则按**自身类型**注册

```csharp
// Program.cs 中一行搞定
services.AutoDependencyInjection();
```

::: warning 注意
使用 `[Service]` 标注的类 **禁止** 在 `Program.cs` 中重复注册，否则会导致重复实例。
:::

## 实际示例

```csharp
// Worker 使用 Transient（定时任务每次创建新的 scope）
[Service(ServiceLifetime.Transient)]
public class DailyWorker
{
    private readonly IDbContextFactory<ArtDbContext> _contextFactory;

    public DailyWorker(IDbContextFactory<ArtDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
}

// 接口注入 — 如果类实现了接口，按接口类型注册
[Service(ServiceLifetime.Singleton)]
public class TaskScheduler : ITaskScheduler, IHostedService
{
    // 可以通过 ITaskScheduler 注入
}
```
