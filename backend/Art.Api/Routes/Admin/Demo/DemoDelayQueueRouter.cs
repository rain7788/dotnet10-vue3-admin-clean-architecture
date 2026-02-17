using Art.Core.Services.Admin.Demo;
using Art.Domain.Constants;
using Art.Infra.Framework.Routes;

namespace Art.Api.Routes.Admin.Demo;

/// <summary>
/// 后台管理 - Demo 延迟消息队列路由
/// </summary>
public class DemoDelayQueueRouter : IAdminRouterBase
{
    public void AddRoutes(RouteGroupBuilder group)
    {
        var g = group.MapGroup("demo/delay-queue")
            .WithGroupName(ApiGroups.Admin)
            .WithTags("Demo-延迟队列");

        g.MapPost("enqueue", (DemoDelayEnqueueRequest request, DemoDelayQueueService service) =>
            Results.Ok(service.Enqueue(request)))
            .WithSummary("Demo：投递延迟消息");

        g.MapPost("enqueue/batch", (DemoDelayBatchEnqueueRequest request, DemoDelayQueueService service) =>
            Results.Ok(service.BatchEnqueue(request)))
            .WithSummary("Demo：批量投递延迟消息（最多100条）");

        g.MapGet("status", (DemoDelayQueueService service) =>
            service.GetStatus())
            .WithSummary("Demo：查询延迟队列状态");

        g.MapGet("preview", (DemoDelayQueueService service, int? count) =>
            service.Preview(count ?? 20))
            .WithSummary("Demo：预览队列中的消息（不消费）");
    }
}
