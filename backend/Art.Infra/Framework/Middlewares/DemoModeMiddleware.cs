using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Art.Domain.Exceptions;

namespace Art.Infra.Framework.Middlewares;

/// <summary>
/// 演示模式中间件
/// 开启后拦截所有写操作（POST/PUT/PATCH/DELETE），仅允许查询和登录
/// 关闭方式：appsettings.json 中设置 Settings:DemoMode 为 false 或删除该配置项
/// </summary>
public class DemoModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _isDemoMode;

    /// <summary>
    /// 不拦截的路径关键词（登录、登出、Token 刷新、分页查询）
    /// </summary>
    private static readonly string[] WhitelistKeywords = ["/login", "/logout", "/token/refresh", "/list"];

    /// <summary>
    /// 不拦截的路径前缀（Demo 功能等允许写操作的路径）
    /// </summary>
    private static readonly string[] WhitelistPrefixes = ["/admin/demo/"];

    public DemoModeMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _isDemoMode = configuration.GetValue<bool>("Settings:DemoMode");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_isDemoMode && IsWriteOperation(context.Request))
        {
            throw new ForbiddenException("演示环境，不允许修改数据");
        }

        await _next(context);
    }

    private static bool IsWriteOperation(HttpRequest request)
    {
        // GET 请求一律放行
        if (HttpMethods.IsGet(request.Method))
            return false;

        var path = request.Path.Value ?? "";

        // 路径前缀白名单放行（如 /admin/demo/ 下的所有接口）
        foreach (var prefix in WhitelistPrefixes)
        {
            if (path.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // POST 关键词白名单放行（登录、查询等）
        if (HttpMethods.IsPost(request.Method))
        {
            foreach (var keyword in WhitelistKeywords)
            {
                if (path.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        // 其余 POST/PUT/PATCH/DELETE 视为写操作
        return true;
    }
}
