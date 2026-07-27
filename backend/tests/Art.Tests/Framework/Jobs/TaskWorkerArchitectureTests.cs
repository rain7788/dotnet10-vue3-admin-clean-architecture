using System.Reflection;
using Art.Core.Workers;
using Art.Infra.Extensions;
using Art.Infra.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Art.Tests.Framework.Jobs;

public class TaskWorkerArchitectureTests
{
    private static readonly Assembly[] ApplicationAssemblies =
    [
        typeof(DailyWorker).Assembly,
        typeof(ServiceAttribute).Assembly
    ];

    [Fact]
    public void Workers_AreMarkedAndRegisteredAsSingletons()
    {
        var workerTypes = GetWorkerTypes();
        var services = new ServiceCollection();
        services.AutoDependencyInjection(typeof(DailyWorker).Assembly);

        Assert.NotEmpty(workerTypes);
        foreach (var workerType in workerTypes)
        {
            Assert.NotNull(workerType.GetCustomAttribute<TaskWorkerAttribute>());

            var descriptor = Assert.Single(services.Where(service => service.ServiceType == workerType));
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }
    }

    [Fact]
    public void Workers_DoNotDependOnScopedServices()
    {
        foreach (var workerType in GetWorkerTypes())
        {
            foreach (var parameter in workerType.GetConstructors().SelectMany(constructor => constructor.GetParameters()))
            {
                Assert.False(
                    IsForbiddenDependency(parameter.ParameterType),
                    $"{workerType.FullName} 不能注入 {parameter.ParameterType.FullName}；TaskWorker 只能依赖单例服务，并应通过工厂创建短生命周期资源。");
            }
        }
    }

    private static Type[] GetWorkerTypes()
    {
        return typeof(DailyWorker).Assembly.GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                           && type.Namespace?.StartsWith("Art.Core.Workers", StringComparison.Ordinal) == true)
            .ToArray();
    }

    private static bool IsForbiddenDependency(Type dependencyType)
    {
        if (dependencyType == typeof(IServiceProvider)
            || dependencyType == typeof(IServiceScopeFactory)
            || dependencyType == typeof(RequestContext)
            || typeof(DbContext).IsAssignableFrom(dependencyType))
            return true;

        return ApplicationAssemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false }
                           && (type == dependencyType || dependencyType.IsAssignableFrom(type)))
            .Select(type => type.GetCustomAttribute<ServiceAttribute>())
            .Any(attribute => attribute?.LifeTime == ServiceLifetime.Scoped);
    }
}
