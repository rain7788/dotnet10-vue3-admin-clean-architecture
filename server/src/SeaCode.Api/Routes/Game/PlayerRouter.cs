using SeaCode.Domain.Constants;
using SeaCode.Infra.Framework.Routes;

namespace SeaCode.Api.Routes.Game;

/// <summary>
/// 玩家路由
/// </summary>
public class PlayerRouter : IGameRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var player = group.MapGroup("player")
            .WithGroupName(ApiGroups.Game)
            .WithTags("玩家");

        player.MapGet("profile", () => Results.Ok(new { message = "TODO: 获取玩家信息" }))
            .WithSummary("获取玩家信息");
    }
}
