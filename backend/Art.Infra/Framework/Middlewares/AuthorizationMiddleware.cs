using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RedisClient = FreeRedis.RedisClient;
using Microsoft.Extensions.Hosting;
using Art.Domain.Constants;
using Art.Domain.Enums;
using Art.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Art.Infra.Data;
using System.Text;
using System.Text.Json;
using Serilog;

namespace Art.Infra.Framework.Middlewares;

/// <summary>
/// 鉴权中间件
/// </summary>
public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationMiddleware> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IDiagnosticContext _diagnosticContext;
    private readonly IDbContextFactory<ArtDbContext> _dbFactory;

    public AuthorizationMiddleware(
        RequestDelegate next,
        IHostEnvironment hostEnvironment,
        ILogger<AuthorizationMiddleware> logger,
        IDiagnosticContext diagnosticContext,
        IDbContextFactory<ArtDbContext> dbFactory)
    {
        _next = next;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _diagnosticContext = diagnosticContext;
        _dbFactory = dbFactory;
    }

    public async Task Invoke(
        HttpContext context,
        ArtDbContext dbContext,
        RequestContext requestContext,
        RedisClient cache)
    {
        // 获取路由元数据
        var apiMeta = context.GetEndpoint()?.Metadata.GetMetadata<ApiMeta>();
        var requiredTokenType = apiMeta?.AuthType;

        // 提取 Token
        var token = ExtractToken(context);
        requestContext.AccessToken = token;
        requestContext.RequestId = context.TraceIdentifier;
        requestContext.RequestIp = context.Connection.RemoteIpAddress?.ToString();
        requestContext.UserAgent = context.Request.Headers.UserAgent.ToString();

        // 提取租户 ID（从 Header 或其他来源）
        ExtractTenantId(context, requestContext);

        // 如果需要鉴权
        if (requiredTokenType != null && requiredTokenType != TokenType.无)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new UnauthorizedException();
            }

            var userInfo = await GetUserInfo(token, dbContext, cache);

            if (userInfo == null || userInfo.TokenType != requiredTokenType)
            {
                throw new UnauthorizedException();
            }

            // 填充用户上下文
            PopulateContext(requestContext, userInfo);
        }
        else if (!string.IsNullOrWhiteSpace(token))
        {
            // 非必须鉴权的接口，如果有 Token 也尝试解析
            var userInfo = await GetUserInfo(token, dbContext, cache);
            if (userInfo != null)
            {
                PopulateContext(requestContext, userInfo);
            }
        }

        // 记录用户信息到 Serilog DiagnosticContext（用于日志字段提取）
        _diagnosticContext.Set("UserId", requestContext.Id > 0 ? requestContext.Id.ToString() : null);
        _diagnosticContext.Set("UserName", requestContext.Name);
        _diagnosticContext.Set("IpAddress", GetClientIpAddress(requestContext, context));

        // 日志记录（使用 LoggerScope）
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["UserId"] = requestContext.Id,
            ["TenantId"] = requestContext.TenantId,
            ["UserName"] = requestContext.Name
        }))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// 提取租户 ID
    /// 优先级：Header > Token 中的租户 > 默认值
    /// </summary>
    private static void ExtractTenantId(HttpContext context, RequestContext requestContext)
    {
        // 从 Header 中获取租户 ID（用于切换租户）
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader))
        {
            var tenantId = tenantHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                requestContext.TenantId = tenantId;
            }
        }

        // 从 Header 中获取是否忽略租户过滤（平台端跨租户查询）
        if (context.Request.Headers.TryGetValue("X-Ignore-Tenant-Filter", out var ignoreHeader))
        {
            if (bool.TryParse(ignoreHeader.FirstOrDefault(), out var ignore))
            {
                requestContext.IgnoreTenantFilter = ignore;
            }
        }
    }

    /// <summary>
    /// 获取客户端 IP 地址
    /// </summary>
    private static string? GetClientIpAddress(RequestContext requestContext, HttpContext context)
    {
        // 优先使用 RequestContext 中已设置的
        if (!string.IsNullOrWhiteSpace(requestContext.RequestIp))
            return requestContext.RequestIp;

        // 优先使用 X-Forwarded-For
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',').FirstOrDefault()?.Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString()?.Replace("::ffff:", string.Empty);
    }

    private static string ExtractToken(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var headerAuth))
        {
            var headerAuthStr = headerAuth.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerAuthStr))
            {
                // 支持 "Bearer xxx" 格式
                if (headerAuthStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return headerAuthStr.Substring("Bearer ".Length).Trim();
                }
                // 也支持直接传 token（如 sc_xxx）
                return headerAuthStr.Trim();
            }
        }
        return string.Empty;
    }

    private async Task<CachedUserInfo?> GetUserInfo(
        string token,
        ArtDbContext dbContext,
        RedisClient cache)
    {
        var tokenHash = ComputeMd5(token);
        var cacheKey = CacheKeys.UserToken(tokenHash);
        var cacheExpire = _hostEnvironment.IsProduction()
            ? TimeSpan.FromMinutes(10)
            : TimeSpan.FromMinutes(1);

        // 尝试从缓存获取
        var cached = cache.Get<string>(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<CachedUserInfo>(cached);
        }

        // 从数据库查询 Token 信息
        var tokenInfo = await dbContext.TokenAccess
            .Where(x => x.Token == token && x.ExpirationTime > DateTime.Now)
            .Select(x => new
            {
                x.UserId,
                x.Type,
                x.Client
            })
            .FirstOrDefaultAsync();

        if (tokenInfo == null)
        {
            return null;
        }

        // 根据 TokenType 查询对应的用户信息
        CachedUserInfo? userInfo = null;

        if (tokenInfo.Type == TokenType.平台端)
        {
            userInfo = await dbContext.SysUser
                .Where(x => x.Id == tokenInfo.UserId && x.Status == ActiveStatus.正常)
                .Select(x => new CachedUserInfo
                {
                    UserId = x.Id,
                    Name = x.RealName ?? x.Username,
                    Account = x.Username,
                    TokenType = TokenType.平台端,
                    ClientType = tokenInfo.Client,
                    IsSuper = x.IsSuper
                })
                .FirstOrDefaultAsync();
        }
        // 如果需要支持玩家端，可以在这里添加
        // else if (tokenInfo.Type == TokenType.玩家端)
        // {
        //     // 查询玩家表...
        // }

        // 写入缓存
        if (userInfo != null)
        {
            var json = JsonSerializer.Serialize(userInfo);
            cache.Set(cacheKey, json, (int)cacheExpire.TotalSeconds);

            // 异步更新最后登录时间（不阻塞主流程，使用独立的 DbContext）
            _ = UpdateLastLoginTimeAsync(tokenInfo.Type, tokenInfo.UserId);
        }

        return userInfo;
    }

    /// <summary>
    /// 异步更新用户最后登录时间
    /// 在缓存刷新时调用，相当于每10分钟更新一次
    /// 使用 IDbContextFactory 创建独立的 DbContext，避免请求结束后被释放
    /// </summary>
    private async Task UpdateLastLoginTimeAsync(TokenType tokenType, long userId)
    {
        try
        {
            await using var dbContext = await _dbFactory.CreateDbContextAsync();
            var now = DateTime.Now;

            if (tokenType == TokenType.平台端)
            {
                await dbContext.SysUser
                    .Where(x => x.Id == userId)
                    .ExecuteUpdateAsync(x => x.SetProperty(u => u.LastLoginTime, now));
            }
            // 如果后续有玩家端，可以在这里扩展
            // else if (tokenType == TokenType.玩家端)
            // {
            //     await dbContext.Player
            //         .Where(x => x.Id == userId)
            //         .ExecuteUpdateAsync(x => x.SetProperty(p => p.LastLoginTime, now));
            // }
        }
        catch (Exception ex)
        {
            // 更新失败不影响主流程，仅记录日志
            _logger.LogWarning(ex, "更新用户最后登录时间失败: UserId={UserId}, TokenType={TokenType}", userId, tokenType);
        }
    }

    private static void PopulateContext(RequestContext ctx, CachedUserInfo info)
    {
        ctx.Id = info.UserId ?? 0;
        ctx.UserId = info.PlayerId ?? 0;
        ctx.Name = info.Name;
        ctx.Account = info.Account;
        ctx.ClientType = info.ClientType;
        ctx.IsSuper = info.IsSuper;

        // 如果用户信息中有租户 ID，且请求头中没有指定，则使用用户的租户
        if (!string.IsNullOrEmpty(info.TenantId) && ctx.TenantId == "default")
        {
            ctx.TenantId = info.TenantId;
        }
    }

    private static string ComputeMd5(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

/// <summary>
/// 缓存的用户信息
/// </summary>
public class CachedUserInfo
{
    public long? UserId { get; set; }
    public long? PlayerId { get; set; }
    public string? Name { get; set; }
    public string? Account { get; set; }
    public TokenType? TokenType { get; set; }
    public ClientType? ClientType { get; set; }

    /// <summary>
    /// 用户所属租户 ID
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// 是否超级管理员
    /// </summary>
    public bool IsSuper { get; set; }
}
