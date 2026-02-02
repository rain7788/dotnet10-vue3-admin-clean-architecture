using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Art.Infra.Framework;

/// <summary>
/// 服务注入扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 按 [Service] 特性自动注入服务
    /// </summary>
    public static IServiceCollection AutoDependencyInjection(this IServiceCollection services, params Assembly[] assemblies)
    {
        var serviceTypes = GetTargetAssemblies(assemblies)
            .SelectMany(a => a.GetExportedTypes())
            .Where(IsServiceType)
            .Distinct();

        foreach (var impl in serviceTypes)
        {
            RegisterService(services, impl);
        }

        return services;
    }

    private static IEnumerable<Assembly> GetTargetAssemblies(Assembly[] assemblies)
        => assemblies.Length > 0
            ? assemblies
            : AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName?.StartsWith("Art") == true);

    private static bool IsServiceType(Type type)
        => type is { IsClass: true, IsAbstract: false }
           && type.IsDefined(typeof(ServiceAttribute), false);

    private static void RegisterService(IServiceCollection services, Type implementationType)
    {
        var attribute = implementationType.GetCustomAttribute<ServiceAttribute>()!;
        var serviceTypes = GetServiceTypes(implementationType);

        foreach (var serviceType in serviceTypes)
        {
            services.Add(new ServiceDescriptor(serviceType, implementationType, attribute.LifeTime));
        }
    }

    private static IEnumerable<Type> GetServiceTypes(Type implementationType)
    {
        var interfaces = implementationType.GetInterfaces()
            .Where(i => !i.IsGenericType && i.Namespace?.StartsWith("System") != true)
            .ToList();

        return interfaces.Count > 0 ? interfaces : [implementationType];
    }
}
