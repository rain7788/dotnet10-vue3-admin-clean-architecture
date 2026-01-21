using SeaCode.Core.Services.Admin.System;
using SeaCode.Domain.Constants;
using SeaCode.Infra.Framework.Routes;

namespace SeaCode.Api.Routes.Admin.System;

/// <summary>
/// 后台管理 - 角色管理路由
/// </summary>
public class SysRoleRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var roleGroup = group.MapGroup("system/role")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("角色管理");

        // 获取角色列表
        roleGroup.MapPost("list", async (RoleListRequest request, SysRoleService service) =>
            await service.GetRoleListAsync(request))
            .WithSummary("获取角色列表");

        // 获取角色选择列表
        roleGroup.MapGet("select", async (SysRoleService service) =>
            await service.GetRoleSelectListAsync())
            .WithSummary("获取角色选择列表");

        // 新增/更新角色
        roleGroup.MapPost("update", async (UpdateRoleRequest request, SysRoleService service) =>
            await service.UpdateRoleAsync(request))
            .WithSummary("新增/更新角色");

        // 删除角色
        roleGroup.MapDelete("{id}", async (long id, SysRoleService service) =>
        {
            await service.DeleteRoleAsync(id);
            return Results.Ok(new { message = "删除成功" });
        })
            .WithSummary("删除角色");
    }
}
