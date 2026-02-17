using System.Reflection;
using Art.Infra.Framework;
using Microsoft.Extensions.DependencyInjection;

namespace Art.Infra.Extensions;

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
            .SelectMany(GetLoadableExportedTypes)
            .Where(IsServiceType)
            .Distinct();

        foreach (var impl in serviceTypes)
        {
            RegisterService(services, impl);
        }

        return services;
    }

    private static IEnumerable<Assembly> GetTargetAssemblies(Assembly[] assemblies)
    {
        if (assemblies.Length > 0)
            return assemblies;

        // 默认：自动扫描所有 Art.* 程序集。
        // 仅依赖 AppDomain.CurrentDomain.GetAssemblies() 会漏掉“尚未加载”的引用程序集，
        // 从而导致 [Service] 标注的类型未注册（例如 Minimal API 的 service 参数变 UNKNOWN）。
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName?.StartsWith("Art") == true)
            .ToList();

        var entry = Assembly.GetEntryAssembly();
        if (entry is null)
            return loaded;

        var referenced = entry.GetReferencedAssemblies()
            .Where(a => a.Name?.StartsWith("Art") == true)
            .Select(a =>
            {
                try
                {
                    return Assembly.Load(a);
                }
                catch
                {
                    return null;
                }
            })
            .OfType<Assembly>()
            .ToList();

        return loaded
            .Concat(referenced)
            .DistinctBy(a => a.FullName);
    }

    private static IEnumerable<Type> GetLoadableExportedTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null).Cast<Type>();
        }
    }

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
