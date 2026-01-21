using Microsoft.AspNetCore.Routing;

namespace SeaCode.Infra.Framework.Routes;

/// <summary>
/// 游戏 API 路由接口（玩家端）
/// </summary>
public interface IGameRouterBase
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
