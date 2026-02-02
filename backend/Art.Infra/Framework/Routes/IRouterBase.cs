using Microsoft.AspNetCore.Routing;

namespace Art.Infra.Framework.Routes;

/// <summary>
/// 应用 API 路由接口
/// </summary>
public interface IAppRouterBase
{
    void AddRoutes(RouteGroupBuilder group);
}

/// <summary>
/// 管理 API 路由接口（后台）
/// </summary>
public interface IAdminRouterBase
{
    void AddRoutes(RouteGroupBuilder group);
}

/// <summary>
/// 公共 API 路由接口（无需鉴权）
/// </summary>
public interface ICommonRouterBase
{
    void AddRoutes(RouteGroupBuilder group);
}
