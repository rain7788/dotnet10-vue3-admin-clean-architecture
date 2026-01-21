using SeaCode.Core.Services.Admin.System;
using SeaCode.Domain.Constants;
using SeaCode.Domain.Enums;
using SeaCode.Domain.Models.Admin;
using SeaCode.Infra.Framework;
using SeaCode.Infra.Framework.Routes;

namespace SeaCode.Api.Routes.Admin.System;

/// <summary>
/// 后台管理 - 系统日志路由
/// </summary>
public class SysLogRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var logGroup = group.MapGroup("system/log")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("系统日志");

        // 获取日志列表
        logGroup.MapPost("list", async (SysLogListRequest request, SysLogService service) =>
            await service.GetListAsync(request))
            .WithMetadata(new ApiMeta { AuthType = TokenType.平台端 })
            .WithSummary("获取日志列表");
    }
}
