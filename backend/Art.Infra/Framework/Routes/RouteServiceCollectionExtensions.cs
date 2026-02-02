using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Art.Infra.Framework.Routes;

/// <summary>
/// 路由注册扩展
/// </summary>
public static class RouteServiceCollectionExtensions
{
    /// <summary>
    /// 自动扫描并注册路由实现
    /// </summary>
    public static IServiceCollection AddApiRouters(this IServiceCollection services, params Assembly[] assemblies)
    {
        var targetAssemblies = assemblies.Length > 0
            ? assemblies.ToList()
            : AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName?.StartsWith("Art") == true)
                .ToList();

        var types = targetAssemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var type in types)
        {
            if (typeof(IAppRouterBase).IsAssignableFrom(type))
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IAppRouterBase), type));
            }

            if (typeof(IAdminRouterBase).IsAssignableFrom(type))
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IAdminRouterBase), type));
            }

            if (typeof(ICommonRouterBase).IsAssignableFrom(type))
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(ICommonRouterBase), type));
            }
        }

        return services;
    }
}
