using Microsoft.Extensions.DependencyInjection;

namespace SeaCode.Infra.Framework;

/// <summary>
/// 服务注入特性
/// 用于标记需要自动注入的服务类
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute : Attribute
{
    public ServiceLifetime LifeTime { get; }

    public ServiceAttribute(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        LifeTime = serviceLifetime;
    }
}
