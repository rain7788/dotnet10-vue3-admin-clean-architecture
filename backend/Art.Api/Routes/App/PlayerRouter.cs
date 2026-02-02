using Art.Domain.Constants;
using Art.Infra.Framework.Routes;

namespace Art.Api.Routes.App;

/// <summary>
/// 玩家路由
/// </summary>
public class PlayerRouter : IAppRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var player = group.MapGroup("player")
            .WithGroupName(ApiGroups.App)
            .WithTags("玩家");

        player.MapGet("profile", () => Results.Ok(new { message = "TODO: 获取玩家信息" }))
            .WithSummary("获取玩家信息");
    }
}
