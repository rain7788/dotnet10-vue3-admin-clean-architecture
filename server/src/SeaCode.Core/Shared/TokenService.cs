using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeaCode.Domain.Entities;
using SeaCode.Domain.Enums;
using SeaCode.Infra.Data;
using SeaCode.Infra.Framework;

namespace SeaCode.Core.Shared;

/// <summary>
/// Token 服务
/// 处理 Token 的创建、刷新和验证
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class TokenService
{
    /// <summary>
    /// AccessToken 有效期（秒）- 1分钟（测试用）
    /// </summary>
    private const int AccessTokenExpireSeconds = 3600;

    /// <summary>
    /// RefreshToken 有效期（秒）- 180天
    /// </summary>
    private const int RefreshTokenExpireSeconds = 15552000;

    /// <summary>
    /// Token 前缀
    /// </summary>
    private const string TokenPrefix = "sc_";

    private readonly GameDbContext _context;

    public TokenService(GameDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 创建 Token（登录时调用）
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="type">Token 类型</param>
    /// <param name="client">客户端类型</param>
    /// <returns>Token 信息</returns>
    public TokenInfo CreateToken(long userId, TokenType type, ClientType? client = null)
    {
        // 创建 RefreshToken
        var refreshToken = new TokenRefresh
        {
            UserId = userId,
            Type = type,
            Client = client,
            Token = Guid.NewGuid().ToString("N"),
            ExpirationTime = DateTime.Now.AddSeconds(RefreshTokenExpireSeconds),
            CreatedTime = DateTime.Now
        };
        _context.TokenRefresh.Add(refreshToken);

        // 创建 AccessToken
        var accessToken = new TokenAccess
        {
            UserId = userId,
            Type = type,
            Client = client,
            Token = $"{TokenPrefix}{Guid.NewGuid():N}",
            ExpirationTime = DateTime.Now.AddSeconds(AccessTokenExpireSeconds),
            CreatedTime = DateTime.Now,
            TokenRefresh = refreshToken
        };
        _context.TokenAccess.Add(accessToken);

        _context.SaveChanges();

        return new TokenInfo
        {
            UserId = userId,
            Token = accessToken.Token,
            RefreshToken = refreshToken.Token,
            TokenExpiresTime = AccessTokenExpireSeconds
        };
    }

    /// <summary>
    /// 刷新 Token
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>新的 Token 信息</returns>
    public TokenInfo? RefreshToken(string refreshToken)
    {
        var rt = _context.TokenRefresh
            .FirstOrDefault(x => x.Token == refreshToken && x.ExpirationTime > DateTime.Now);

        if (rt == null)
        {
            return null;
        }

        // 更新 RefreshToken
        rt.Token = Guid.NewGuid().ToString("N");
        rt.ExpirationTime = DateTime.Now.AddSeconds(RefreshTokenExpireSeconds);

        // 创建新的 AccessToken
        var accessToken = new TokenAccess
        {
            UserId = rt.UserId,
            Type = rt.Type,
            Client = rt.Client,
            Token = $"{TokenPrefix}{Guid.NewGuid():N}",
            ExpirationTime = DateTime.Now.AddSeconds(AccessTokenExpireSeconds),
            CreatedTime = DateTime.Now,
            RefreshTokenId = rt.Id
        };
        _context.TokenAccess.Add(accessToken);

        _context.SaveChanges();

        return new TokenInfo
        {
            UserId = rt.UserId,
            Token = accessToken.Token,
            RefreshToken = rt.Token,
            TokenExpiresTime = AccessTokenExpireSeconds
        };
    }

    /// <summary>
    /// 验证 AccessToken 并获取 Token 信息
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <returns>Token 信息，如果无效则返回 null</returns>
    public async Task<TokenAccessInfo?> ValidateAccessTokenAsync(string accessToken)
    {
        var tokenInfo = await _context.TokenAccess
            .Where(x => x.Token == accessToken && x.ExpirationTime > DateTime.Now)
            .Select(x => new TokenAccessInfo
            {
                UserId = x.UserId,
                Type = x.Type,
                Client = x.Client
            })
            .FirstOrDefaultAsync();

        return tokenInfo;
    }

    /// <summary>
    /// 注销（使 Token 失效）
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    public async Task RevokeTokenAsync(string accessToken)
    {
        var token = await _context.TokenAccess
            .FirstOrDefaultAsync(x => x.Token == accessToken);

        if (token != null)
        {
            // 将过期时间设置为当前时间，使其立即失效
            token.ExpirationTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}

/// <summary>
/// Token 信息（用于登录/刷新响应）
/// </summary>
public class TokenInfo
{
    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 访问令牌
    /// </summary>
    public string Token { get; set; } = default!;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = default!;

    /// <summary>
    /// Token 过期时间（秒）
    /// </summary>
    public int TokenExpiresTime { get; set; }
}

/// <summary>
/// Token 访问信息（用于验证）
/// </summary>
public class TokenAccessInfo
{
    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Token 类型
    /// </summary>
    public TokenType Type { get; set; }

    /// <summary>
    /// 客户端类型
    /// </summary>
    public ClientType? Client { get; set; }
}
