using SeaCode.Domain.Constants;
using SeaCode.Infra.Framework.Routes;

namespace SeaCode.Api.Routes.Common;

/// <summary>
/// 健康检查路由
/// </summary>
public class HealthRouter : ICommonRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var health = group.MapGroup("health")
            .WithGroupName(ApiGroups.Common)
            .WithTags("健康检查");

        health.MapGet("", () => Results.Ok(new { status = "healthy", time = DateTime.Now }))
            .WithSummary("健康检查");

        health.MapGet("ping", () => "pong")
            .WithSummary("Ping");
    }
}
