using Art.Domain.Constants;
using Art.Infra.Framework.Routes;

namespace Art.Api.Routes.Admin.App;

/// <summary>
/// 后台管理 - 玩家管理路由
/// </summary>
public class PlayerManageRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var players = group.MapGroup("players")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("玩家管理");

        players.MapGet("list", () => Results.Ok(new { message = "TODO: 获取玩家列表" }))
            .WithSummary("获取玩家列表");
    }
}
