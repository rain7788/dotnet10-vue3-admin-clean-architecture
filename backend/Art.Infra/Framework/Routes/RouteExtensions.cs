using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Art.Domain.Constants;
using Art.Domain.Enums;

namespace Art.Infra.Framework.Routes;

/// <summary>
/// 路由配置扩展
/// </summary>
public static class RouteExtensions
{
    /// <summary>
    /// 配置 API 分组
    /// </summary>
    public static void ConfigureApiGroups(this WebApplication app)
    {
        var api = app.MapGroup(string.Empty);

        // 应用 API - 需要客户端 Token
        var appGroup = api.MapGroup(ApiGroups.App)
            .WithGroupName(ApiGroups.App)
            .WithMetadata(new ApiMeta { AuthType = TokenType.玩家端 })
            .WithTags("应用");

        // 管理 API - 需要平台端 Token
        var adminGroup = api.MapGroup(ApiGroups.Admin)
            .WithGroupName(ApiGroups.Admin)
            .WithMetadata(new ApiMeta { AuthType = TokenType.平台端 })
            .WithTags("管理");

        // 公共 API - 无需鉴权
        var commonGroup = api.MapGroup(ApiGroups.Common)
            .WithGroupName(ApiGroups.Common)
            .WithTags("公共");

        // 注册应用路由
        var appRouters = app.Services.GetServices<IAppRouterBase>();
        foreach (var router in appRouters)
        {
            router.AddRoutes(appGroup);
        }

        // 注册管理路由
        var adminRouters = app.Services.GetServices<IAdminRouterBase>();
        foreach (var router in adminRouters)
        {
            router.AddRoutes(adminGroup);
        }

        // 注册公共路由
        var commonRouters = app.Services.GetServices<ICommonRouterBase>();
        foreach (var router in commonRouters)
        {
            router.AddRoutes(commonGroup);
        }
    }

    /// <summary>
    /// 设置接口无需鉴权
    /// </summary>
    public static RouteHandlerBuilder AllowAnonymous(this RouteHandlerBuilder builder)
    {
        return builder.WithMetadata(new ApiMeta { AuthType = TokenType.无 });
    }

    /// <summary>
    /// 设置接口需要指定 Token 类型
    /// </summary>
    public static RouteHandlerBuilder RequireAuth(this RouteHandlerBuilder builder, TokenType tokenType)
    {
        return builder.WithMetadata(new ApiMeta { AuthType = tokenType });
    }
}
