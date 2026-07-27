using Microsoft.Extensions.DependencyInjection;

namespace Art.Infra.Framework;

/// <summary>
/// 标记由任务调度器长期持有的无状态 Worker，并固定使用单例生命周期。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TaskWorkerAttribute : ServiceAttribute
{
    public TaskWorkerAttribute()
        : base(ServiceLifetime.Singleton)
    {
    }
}
