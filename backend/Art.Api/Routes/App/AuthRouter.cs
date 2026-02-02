using Art.Domain.Constants;
using Art.Infra.Framework.Routes;

namespace Art.Api.Routes.App;

/// <summary>
/// 认证路由
/// </summary>
public class AuthRouter : IAppRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var auth = group.MapGroup("auth")
            .WithGroupName(ApiGroups.App)
            .WithTags("认证");

        auth.MapPost("login", () => Results.Ok(new { message = "TODO: 实现登录" }))
            .WithSummary("玩家登录")
            .AllowAnonymous();

        auth.MapPost("logout", () => Results.Ok(new { message = "TODO: 实现登出" }))
            .WithSummary("玩家登出");
    }
}
