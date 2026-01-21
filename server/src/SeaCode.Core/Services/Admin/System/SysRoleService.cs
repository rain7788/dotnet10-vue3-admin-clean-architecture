using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeaCode.Domain.Entities;
using SeaCode.Domain.Enums;
using SeaCode.Domain.Exceptions;
using SeaCode.Infra.Data;
using SeaCode.Infra.Framework;

namespace SeaCode.Core.Services.Admin.System;

/// <summary>
/// 角色管理服务
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class SysRoleService
{
    private readonly GameDbContext _context;
    private readonly RequestContext _user;

    public SysRoleService(GameDbContext context, RequestContext user)
    {
        _context = context;
        _user = user;
    }

    /// <summary>
    /// 获取角色列表
    /// </summary>
    public async Task<RoleListResponse> GetRoleListAsync(RoleListRequest request)
    {
        // 使用 PredicateBuilder 构建动态条件
        var predicate = PredicateBuilder.New<SysRole>(true);

        if (!string.IsNullOrWhiteSpace(request.Name))
            predicate = predicate.And(x => x.Name.Contains(request.Name));

        if (!string.IsNullOrWhiteSpace(request.Code))
            predicate = predicate.And(x => x.Code.Contains(request.Code));

        if (request.Status.HasValue)
            predicate = predicate.And(x => x.Status == request.Status.Value);

        var query = _context.SysRole
            .AsExpandable()
            .Where(predicate)
            .OrderByDescending(x => x.CreatedTime);

        // 总数
        var total = await query.CountAsync();

        // 分页
        var pageIndex = request.PageIndex ?? 1;
        var pageSize = request.PageSize ?? 20;
        var roles = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RoleListItem
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Description = x.Description,
                Status = x.Status,
                CreatedTime = x.CreatedTime
            })
            .ToListAsync();

        // 获取角色菜单信息
        var roleIds = roles.Select(x => x.Id).ToList();
        var roleMenus = await _context.SysRoleMenu
            .Where(x => roleIds.Contains(x.RoleId))
            .GroupBy(x => x.RoleId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.MenuId).ToList());

        var rolePermissions = await _context.SysRolePermission
            .Where(x => roleIds.Contains(x.RoleId))
            .GroupBy(x => x.RoleId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.PermissionId).ToList());

        foreach (var role in roles)
        {
            role.MenuIds = roleMenus.TryGetValue(role.Id, out var menus) ? menus : new();
            role.PermissionIds = rolePermissions.TryGetValue(role.Id, out var perms) ? perms : new();
        }

        return new RoleListResponse
        {
            Total = total,
            Items = roles
        };
    }

    /// <summary>
    /// 获取所有角色（用于下拉选择）
    /// </summary>
    public async Task<List<RoleSelectItem>> GetRoleSelectListAsync()
    {
        return await _context.SysRole
            .Where(x => x.Status == ActiveStatus.正常)
            .OrderByDescending(x => x.CreatedTime)
            .Select(x => new RoleSelectItem
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code
            })
            .ToListAsync();
    }

    /// <summary>
    /// 新增/更新角色
    /// </summary>
    public async Task<UpdateRoleResponse> UpdateRoleAsync(UpdateRoleRequest request)
    {
        SysRole? role = null;

        // 有 ID 则为更新
        if (request.Id.HasValue)
        {
            role = await _context.SysRole.FirstOrDefaultAsync(x => x.Id == request.Id.Value);
            if (role == null)
                throw new BadRequestException("角色不存在");

            // 检查编码是否重复（排除当前）
            if (!string.IsNullOrWhiteSpace(request.Code) &&
                await _context.SysRole.AnyAsync(x => x.Code == request.Code && x.Id != request.Id.Value))
                throw new BadRequestException("角色编码已存在");
        }
        else
        {
            // 新增
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new BadRequestException("角色名称不能为空");

            if (string.IsNullOrWhiteSpace(request.Code))
                throw new BadRequestException("角色编码不能为空");

            if (await _context.SysRole.AnyAsync(x => x.Code == request.Code))
                throw new BadRequestException("角色编码已存在");

            role = new SysRole();
            _context.Add(role);
        }

        // 更新字段
        role.Name = request.Name ?? role.Name;
        role.Code = request.Code ?? role.Code;
        role.Description = request.Description ?? role.Description;
        role.Status = request.Status ?? role.Status;

        await _context.SaveChangesAsync();

        // 处理角色菜单关联
        if (request.MenuIds != null)
        {
            var existingMenus = await _context.SysRoleMenu.Where(x => x.RoleId == role.Id).ToListAsync();
            if (existingMenus.Any())
                _context.RemoveRange(existingMenus);

            foreach (var menuId in request.MenuIds.Distinct())
            {
                _context.Add(new SysRoleMenu
                {
                    RoleId = role.Id,
                    MenuId = menuId
                });
            }
        }

        // 处理角色权限关联
        if (request.PermissionIds != null)
        {
            var existingPerms = await _context.SysRolePermission.Where(x => x.RoleId == role.Id).ToListAsync();
            if (existingPerms.Any())
                _context.RemoveRange(existingPerms);

            foreach (var permId in request.PermissionIds.Distinct())
            {
                _context.Add(new SysRolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });
            }
        }

        await _context.SaveChangesAsync();

        return new UpdateRoleResponse { Id = role.Id };
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    public async Task<bool> DeleteRoleAsync(long id)
    {
        var role = await _context.SysRole.FirstOrDefaultAsync(x => x.Id == id);
        if (role == null)
            throw new BadRequestException("角色不存在");

        // 检查角色是否被用户使用
        if (await _context.SysUserRole.AnyAsync(x => x.RoleId == id))
            throw new BadRequestException("该角色已被分配给用户，无法删除");

        // 删除角色菜单关联
        var roleMenus = await _context.SysRoleMenu.Where(x => x.RoleId == id).ToListAsync();
        if (roleMenus.Any())
            _context.RemoveRange(roleMenus);

        // 删除角色权限关联
        var rolePerms = await _context.SysRolePermission.Where(x => x.RoleId == id).ToListAsync();
        if (rolePerms.Any())
            _context.RemoveRange(rolePerms);

        _context.Remove(role);
        await _context.SaveChangesAsync();

        return true;
    }
}

#region 请求/响应模型

/// <summary>
/// 角色列表请求
/// </summary>
public class RoleListRequest
{
    /// <summary>
    /// 角色名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 角色编码
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ActiveStatus? Status { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int? PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页条数
    /// </summary>
    public int? PageSize { get; set; } = 20;
}

/// <summary>
/// 角色列表响应
/// </summary>
public class RoleListResponse
{
    public int Total { get; set; }
    public List<RoleListItem> Items { get; set; } = new();
}

/// <summary>
/// 角色列表项
/// </summary>
public class RoleListItem
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Description { get; set; }
    public ActiveStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public DateTime CreatedTime { get; set; }
    public List<long> MenuIds { get; set; } = new();
    public List<long> PermissionIds { get; set; } = new();
}

/// <summary>
/// 角色选择项
/// </summary>
public class RoleSelectItem
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
}

/// <summary>
/// 新增/更新角色请求
/// </summary>
public class UpdateRoleRequest
{
    /// <summary>
    /// 角色ID（新增时为空）
    /// </summary>
    public long? Id { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 角色编码
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ActiveStatus? Status { get; set; }

    /// <summary>
    /// 菜单ID列表
    /// </summary>
    public List<long>? MenuIds { get; set; }

    /// <summary>
    /// 权限ID列表
    /// </summary>
    public List<long>? PermissionIds { get; set; }
}

/// <summary>
/// 更新角色响应
/// </summary>
public class UpdateRoleResponse
{
    public long Id { get; set; }
}

#endregion
