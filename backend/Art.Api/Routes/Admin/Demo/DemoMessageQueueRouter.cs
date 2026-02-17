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
    }
}
