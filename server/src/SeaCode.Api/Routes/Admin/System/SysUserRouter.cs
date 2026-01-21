using SeaCode.Core.Services.Admin.System;
using SeaCode.Domain.Constants;
using SeaCode.Domain.Enums;
using SeaCode.Infra.Framework;
using SeaCode.Infra.Framework.Routes;

namespace SeaCode.Api.Routes.Admin.System;

/// <summary>
/// 后台管理 - 系统用户路由
/// </summary>
public class SysUserRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var userGroup = group.MapGroup("system/user")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("系统用户");

        // 登录（无需鉴权）
        userGroup.MapPost("login", async (LoginRequest request, SysUserService service) =>
            await service.LoginAsync(request))
            .WithMetadata(new ApiMeta { AuthType = TokenType.无 })
            .WithSummary("登录");

        // 刷新 Token（无需鉴权）
        userGroup.MapPost("token/refresh", (RefreshTokenRequest request, SysUserService service) =>
            service.RefreshToken(request.RefreshToken))
            .WithMetadata(new ApiMeta { AuthType = TokenType.无 })
            .WithSummary("刷新Token");

        // 获取当前用户信息
        userGroup.MapGet("info", async (SysUserService service) =>
            await service.GetCurrentUserAsync())
            .WithSummary("获取当前用户信息");

        // 修改密码
        userGroup.MapPost("password", async (ChangePasswordRequest request, SysUserService service) =>
        {
            await service.ChangePasswordAsync(request);
            return Results.Ok(new { message = "密码修改成功" });
        })
            .WithSummary("修改密码");

        // 获取用户列表
        userGroup.MapPost("list", async (UserListRequest request, SysUserService service) =>
            await service.GetUserListAsync(request))
            .WithSummary("获取用户列表");

        // 新增/更新用户
        userGroup.MapPost("update", async (UpdateUserRequest request, SysUserService service) =>
            await service.UpdateUserAsync(request))
            .WithSummary("新增/更新用户");

        // 删除用户
        userGroup.MapDelete("{id}", async (long id, SysUserService service) =>
        {
            await service.DeleteUserAsync(id);
            return Results.Ok(new { message = "删除成功" });
        })
            .WithSummary("删除用户");
    }
}
