using SeaCode.Domain.Constants;
using SeaCode.Infra.Framework.Routes;

namespace SeaCode.Api.Routes.Admin.Game;

/// <summary>
/// 后台管理 - 仪表盘路由
/// </summary>
public class DashboardRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var dashboard = group.MapGroup("dashboard")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("仪表盘");

        dashboard.MapGet("stats", () => new
        {
            // TODO: 注入统计服务
            TotalUsers = 0,
            TotalPlayers = 0,
            OnlinePlayers = 0,
            TotalTransactions = 0,
            UpdateTime = DateTime.Now
        })
        .WithSummary("获取统计数据");
    }
}
