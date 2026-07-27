using Microsoft.Extensions.DependencyInjection;

namespace Art.Infra.Framework.Jobs;

public static class TaskSchedulerServiceCollectionExtensions
{
    /// <summary>
    /// 注册唯一的任务调度器实例，并将调度接口和 Host 生命周期映射到该实例。
    /// </summary>
    public static IServiceCollection AddTaskScheduler(this IServiceCollection services)
    {
        services.AddSingleton<TaskScheduler>();
        services.AddSingleton<ITaskScheduler>(sp => sp.GetRequiredService<TaskScheduler>());
        services.AddHostedService(sp => sp.GetRequiredService<TaskScheduler>());
        return services;
    }
}
