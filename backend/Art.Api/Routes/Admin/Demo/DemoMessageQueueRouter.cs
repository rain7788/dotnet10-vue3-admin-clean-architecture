using Art.Core.Services.Admin.Demo;
using Art.Domain.Constants;
using Art.Infra.Framework.Routes;

namespace Art.Api.Routes.Admin.Demo;

/// <summary>
/// 后台管理 - Demo 消息队列路由
/// </summary>
public class DemoMessageQueueRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var g = group.MapGroup("demo/queue")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("Demo-消息队列");

        // 入队：LPUSH
        g.MapPost("enqueue", async (DemoEnqueueMessageRequest request, DemoMessageQueueService service) =>
        {
            await service.EnqueueAsync(request);
            return Results.Ok(new { message = "已入队" });
        })
            .WithSummary("Demo：入队消息（Redis LPUSH）");

        // 批量入队：LPUSH（最多100条）
        g.MapPost("enqueue/batch", async (DemoBatchEnqueueMessageRequest request, DemoMessageQueueService service) =>
        {
            await service.BatchEnqueueAsync(request);
            return Results.Ok(new { message = "已批量入队" });
        })
            .WithSummary("Demo：批量入队消息（最多100条）");

        // 查询队列状态
        g.MapGet("status", async (DemoMessageQueueService service) =>
            await service.GetQueueStatusAsync())
            .WithSummary("Demo：查询队列状态（队列长度）");
    }
}
