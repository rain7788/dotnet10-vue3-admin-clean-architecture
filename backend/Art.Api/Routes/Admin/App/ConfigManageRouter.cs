using Art.Domain.Constants;
using Art.Infra.Framework.Routes;

namespace Art.Api.Routes.Admin.App;

/// <summary>
/// 后台管理 - 配置管理路由
/// </summary>
public class ConfigManageRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var config = group.MapGroup("config")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("配置管理");

        config.MapGet("list", () => Results.Ok(new { message = "TODO: 获取配置列表" }))
            .WithSummary("获取配置列表");

        config.MapPost("update", () => Results.Ok(new { message = "TODO: 更新配置" }))
            .WithSummary("更新配置");
    }
}
