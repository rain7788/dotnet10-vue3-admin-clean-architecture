using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RedisClient = FreeRedis.RedisClient;
using Microsoft.Extensions.DependencyInjection;
using SeaCode.Core.Shared;
using SeaCode.Domain.Entities;
using SeaCode.Domain.Enums;
using SeaCode.Domain.Exceptions;
using SeaCode.Infra.Data;
using SeaCode.Infra.Framework;

namespace SeaCode.Core.Services.Admin.System;

/// <summary>
/// 系统用户服务
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class SysUserService
{
    private readonly GameDbContext _context;
    private readonly RequestContext _user;
    private readonly TokenService _tokenService;
    private readonly RedisClient _cache;

    public SysUserService(
        GameDbContext context,
        RequestContext user,
        TokenService tokenService,
        RedisClient cache)
    {
        _context = context;
        _user = user;
        _tokenService = tokenService;
        _cache = cache;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var username = request.Username?.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BadRequestException("用户名和密码不能为空");
        }

        // 检查登录失败次数
        var failKey = $"login:fail:{username}:{DateTime.Now:yyyyMMdd}";
        var failCountStr = _cache.Get<string>(failKey);
        var failCount = string.IsNullOrEmpty(failCountStr) ? 0 : int.Parse(failCountStr);

        const int maxFailCount = 10;
        if (failCount >= maxFailCount)
        {
            throw new BadRequestException("今日密码错误次数过多，禁止登录");
        }

        // 查询用户
        var user = await _context.SysUser
            .FirstOrDefaultAsync(x => x.Username == username);

        if (user == null)
        {
            throw new BadRequestException("用户不存在");
        }

        if (user.Status != ActiveStatus.正常)
        {
            throw new BadRequestException("用户已被禁用");
        }

        // 验证密码（密码使用 SHA256 + Base64 加密存储）
        var passwordHash = ComputePasswordHash(request.Password);
        if (user.Password != passwordHash)
        {
            // 记录失败次数
            failCount++;
            _cache.Set(failKey, failCount.ToString(), (int)TimeSpan.FromDays(1).TotalSeconds);

            throw new BadRequestException($"密码错误，今日剩余失败次数：{maxFailCount - failCount}");
        }

        // 创建 Token
        var tokenInfo = _tokenService.CreateToken(user.Id, TokenType.平台端, ClientType.未知);

        // 更新最后登录时间
        user.LastLoginTime = DateTime.Now;
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            UserId = user.Id,
            Username = user.Username,
            RealName = user.RealName,
            Token = tokenInfo.Token,
            RefreshToken = tokenInfo.RefreshToken,
            TokenExpiresTime = tokenInfo.TokenExpiresTime
        };
    }

    /// <summary>
    /// 刷新 Token
    /// </summary>
    public TokenInfo RefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException("RefreshToken 不能为空");
        }

        var tokenInfo = _tokenService.RefreshToken(refreshToken);
        if (tokenInfo == null)
        {
            throw new UnauthorizedException("RefreshToken 已过期或无效");
        }

        return tokenInfo;
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    public async Task<UserInfoResponse> GetCurrentUserAsync()
    {
        var user = await _context.SysUser
            .Where(x => x.Id == _user.Id)
            .Select(x => new UserInfoResponse
            {
                UserId = x.Id,
                Username = x.Username,
                RealName = x.RealName,
                Phone = x.Phone,
                Avatar = x.Avatar,
                IsSuper = x.IsSuper,
                Status = x.Status,
                LastLoginTime = x.LastLoginTime,
                CreatedTime = x.CreatedTime
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            throw new NotFoundException("用户不存在");
        }

        return user;
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            throw new BadRequestException("旧密码和新密码不能为空");

        var user = await _context.SysUser.FirstOrDefaultAsync(x => x.Id == _user.Id);
        if (user == null)
        {
            throw new NotFoundException("用户不存在");
        }

        var oldPasswordHash = ComputePasswordHash(request.OldPassword);
        if (user.Password != oldPasswordHash)
            throw new BadRequestException("旧密码错误");

        user.Password = ComputePasswordHash(request.NewPassword);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    public async Task<UserListResponse> GetUserListAsync(UserListRequest request)
    {
        // 使用 PredicateBuilder 构建动态条件
        var predicate = PredicateBuilder.New<SysUser>(true);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            predicate = predicate.And(x =>
                x.Username.Contains(request.Keyword) ||
                (x.RealName != null && x.RealName.Contains(request.Keyword)) ||
                (x.Phone != null && x.Phone.Contains(request.Keyword)));
        }

        if (request.Status.HasValue)
            predicate = predicate.And(x => x.Status == request.Status.Value);

        var query = _context.SysUser
            .AsExpandable()
            .Where(predicate)
            .OrderByDescending(x => x.CreatedTime);

        // 总数
        var total = await query.CountAsync();

        // 分页
        var pageIndex = request.PageIndex ?? 1;
        var pageSize = request.PageSize ?? 20;
        var users = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserListItem
            {
                UserId = x.Id,
                Username = x.Username,
                RealName = x.RealName,
                Phone = x.Phone,
                Avatar = x.Avatar,
                IsSuper = x.IsSuper,
                Status = x.Status,
                LastLoginTime = x.LastLoginTime,
                CreatedTime = x.CreatedTime
            })
            .ToListAsync();

        // 获取用户角色信息
        var userIds = users.Select(x => x.UserId).ToList();
        var userRoles = await _context.SysUserRole
            .Where(x => userIds.Contains(x.UserId))
            .Join(_context.SysRole, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleId = r.Id, RoleName = r.Name })
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(g => g.Key, g => g.ToList());

        foreach (var user in users)
        {
            if (userRoles.TryGetValue(user.UserId, out var roles))
            {
                user.RoleIds = roles.Select(x => x.RoleId).ToList();
                user.RoleNames = roles.Select(x => x.RoleName).ToList();
            }
        }

        return new UserListResponse
        {
            Total = total,
            Items = users
        };
    }

    /// <summary>
    /// 新增/更新用户
    /// </summary>
    public async Task<UpdateUserResponse> UpdateUserAsync(UpdateUserRequest request)
    {
        SysUser? user = null;

        // 有 ID 则为更新
        if (request.Id.HasValue)
        {
            user = await _context.SysUser.FirstOrDefaultAsync(x => x.Id == request.Id.Value);
            if (user == null)
                throw new BadRequestException("用户不存在");

            // 超级管理员不能修改状态
            if (user.IsSuper && request.Status.HasValue && request.Status.Value != ActiveStatus.正常)
                throw new BadRequestException("超级管理员状态不能修改");

            // 检查用户名是否重复（排除当前用户）
            if (!string.IsNullOrWhiteSpace(request.Username) &&
                await _context.SysUser.AnyAsync(x => x.Username == request.Username && x.Id != request.Id.Value))
                throw new BadRequestException("用户名已存在");
        }
        else
        {
            // 新增
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new BadRequestException("用户名不能为空");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new BadRequestException("密码不能为空");

            if (await _context.SysUser.AnyAsync(x => x.Username == request.Username))
                throw new BadRequestException("用户名已存在");

            user = new SysUser();
            _context.Add(user);
        }

        // 更新字段
        user.Username = request.Username ?? user.Username;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.Password = ComputePasswordHash(request.Password);
        user.RealName = request.RealName ?? user.RealName;
        user.Phone = request.Phone ?? user.Phone;
        user.Avatar = request.Avatar ?? user.Avatar;
        user.Status = request.Status ?? user.Status;

        await _context.SaveChangesAsync();

        // 处理用户角色关联
        if (request.RoleIds != null)
        {
            // 删除现有角色关联
            var existingRoles = await _context.SysUserRole.Where(x => x.UserId == user.Id).ToListAsync();
            if (existingRoles.Any())
                _context.RemoveRange(existingRoles);

            // 添加新角色关联
            foreach (var roleId in request.RoleIds.Distinct())
            {
                _context.Add(new SysUserRole
                {
                    UserId = user.Id,
                    RoleId = roleId
                });
            }

            await _context.SaveChangesAsync();
        }

        return new UpdateUserResponse { Id = user.Id };
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public async Task<bool> DeleteUserAsync(long id)
    {
        var user = await _context.SysUser.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
            throw new BadRequestException("用户不存在");

        if (user.IsSuper)
            throw new BadRequestException("超级管理员不能删除");

        // 删除用户角色关联
        var userRoles = await _context.SysUserRole.Where(x => x.UserId == user.Id).ToListAsync();
        if (userRoles.Any())
            _context.RemoveRange(userRoles);

        _context.Remove(user);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 计算密码哈希（SHA256 + Base64）
    /// </summary>
    private static string ComputePasswordHash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}

#region 请求/响应模型

/// <summary>
/// 登录请求
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// 登录响应
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = default!;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

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
/// 刷新 Token 请求
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = default!;
}

/// <summary>
/// 用户信息响应
/// </summary>
public class UserInfoResponse
{
    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = default!;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 是否超级管理员
    /// </summary>
    public bool IsSuper { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ActiveStatus Status { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }
}

/// <summary>
/// 修改密码请求
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// 旧密码
    /// </summary>
    public string? OldPassword { get; set; }

    /// <summary>
    /// 新密码
    /// </summary>
    public string? NewPassword { get; set; }
}

/// <summary>
/// 用户列表请求
/// </summary>
public class UserListRequest
{
    /// <summary>
    /// 关键词（用户名/姓名/手机号）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 状态筛选
    /// </summary>
    public ActiveStatus? Status { get; set; }

    /// <summary>
    /// 页码（从 1 开始）
    /// </summary>
    public int? PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页条数
    /// </summary>
    public int? PageSize { get; set; } = 20;
}

/// <summary>
/// 用户列表响应
/// </summary>
public class UserListResponse
{
    /// <summary>
    /// 总数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 列表数据
    /// </summary>
    public List<UserListItem> Items { get; set; } = new();
}

/// <summary>
/// 用户列表项
/// </summary>
public class UserListItem
{
    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = default!;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 是否超级管理员
    /// </summary>
    public bool IsSuper { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ActiveStatus Status { get; set; }

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText => Status.ToString();

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 角色ID列表
    /// </summary>
    public List<long> RoleIds { get; set; } = new();

    /// <summary>
    /// 角色名称列表
    /// </summary>
    public List<string> RoleNames { get; set; } = new();
}

/// <summary>
/// 新增/更新用户请求
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// 用户ID（新增时为空）
    /// </summary>
    public long? Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ActiveStatus? Status { get; set; }

    /// <summary>
    /// 角色ID列表
    /// </summary>
    public List<long>? RoleIds { get; set; }
}

/// <summary>
/// 更新用户响应
/// </summary>
public class UpdateUserResponse
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long Id { get; set; }
}

#endregion
