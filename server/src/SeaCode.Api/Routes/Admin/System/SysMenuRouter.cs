using SeaCode.Core.Services.Admin.System;
using SeaCode.Domain.Constants;
using SeaCode.Infra.Framework.Routes;

namespace SeaCode.Api.Routes.Admin.System;

/// <summary>
/// 后台管理 - 菜单管理路由
/// </summary>
public class SysMenuRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var menuGroup = group.MapGroup("system/menu")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("菜单管理");

        // 获取菜单树（管理端）
        menuGroup.MapGet("tree", async (SysMenuService service) =>
            await service.GetMenuTreeAsync())
            .WithSummary("获取菜单树");

        // 获取用户菜单（根据权限过滤）
        menuGroup.MapGet("user", async (SysMenuService service) =>
            await service.GetUserMenuAsync())
            .WithSummary("获取用户菜单");

        // 新增/更新菜单
        menuGroup.MapPost("update", async (UpdateMenuRequest request, SysMenuService service) =>
            await service.UpdateMenuAsync(request))
            .WithSummary("新增/更新菜单");

        // 删除菜单
        menuGroup.MapDelete("{id}", async (long id, SysMenuService service) =>
        {
            await service.DeleteMenuAsync(id);
            return Results.Ok(new { message = "删除成功" });
        })
            .WithSummary("删除菜单");

        // 新增/更新权限
        menuGroup.MapPost("permission/update", async (UpdatePermissionRequest request, SysMenuService service) =>
            await service.UpdatePermissionAsync(request))
            .WithSummary("新增/更新权限");

        // 删除权限
        menuGroup.MapDelete("permission/{id}", async (long id, SysMenuService service) =>
        {
            await service.DeletePermissionAsync(id);
            return Results.Ok(new { message = "删除成功" });
        })
            .WithSummary("删除权限");
    }
}
