using Art.Core.Services.Admin.Demo;
using Art.Domain.Constants;
using Art.Infra.Framework.Routes;

namespace Art.Api.Routes.Admin.Demo;

/// <summary>
/// 后台管理 - Demo 分布式锁路由
/// </summary>
public class DemoDistributedLockRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var g = group.MapGroup("demo/lock")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("Demo-分布式锁");

        // TryLock：立即返回
        g.MapPost("try", async (DemoTryLockRequest request, DemoDistributedLockService service) =>
            await service.TryLockDemoAsync(request))
            .WithSummary("Demo：TryLock 立即尝试获取锁");

        // LockAsync：等待获取
        g.MapPost("wait", async (DemoWaitLockRequest request, DemoDistributedLockService service) =>
            await service.WaitLockDemoAsync(request))
            .WithSummary("Demo：LockAsync 等待获取锁");

        // 查询锁状态
        g.MapGet("status", async (DemoDistributedLockService service) =>
            await service.GetLockStatusAsync())
            .WithSummary("Demo：查询当前锁状态");
    }
}
