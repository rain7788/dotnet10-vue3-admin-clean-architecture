using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeaCode.Domain.Entities;
using SeaCode.Domain.Enums;
using SeaCode.Domain.Exceptions;
using SeaCode.Infra.Data;
using SeaCode.Infra.Framework;

namespace SeaCode.Core.Services.Admin.System;

/// <summary>
/// 菜单管理服务
/// </summary>
[Service(ServiceLifetime.Scoped)]
public class SysMenuService
{
    private readonly GameDbContext _context;
    private readonly RequestContext _user;

    public SysMenuService(GameDbContext context, RequestContext user)
    {
        _context = context;
        _user = user;
    }

    /// <summary>
    /// 获取菜单树（管理端，包含所有菜单）
    /// </summary>
    public async Task<List<MenuTreeItem>> GetMenuTreeAsync()
    {
        var menus = await _context.SysMenu
            .Where(x => x.Status == ActiveStatus.正常)
            .OrderBy(x => x.Sort)
            .Select(x => new MenuTreeItem
            {
                Id = x.Id,
                ParentId = x.ParentId,
                Name = x.Name,
                Code = x.Code,
                Path = x.Path,
                Component = x.Component,
                Icon = x.Icon,
                Sort = x.Sort,
                IsVisible = x.IsVisible,
                IsExternal = x.IsExternal,
                IsIframe = x.IsIframe,
                IsFullPage = x.IsFullPage,
                KeepAlive = x.KeepAlive,
                Link = x.Link,
                Roles = x.Roles,
                ShowBadge = x.ShowBadge,
                ShowTextBadge = x.ShowTextBadge,
                FixedTab = x.FixedTab,
                IsHideTab = x.IsHideTab,
                ActivePath = x.ActivePath,
                Status = x.Status,
                CreatedTime = x.CreatedTime,
                UpdatedTime = x.UpdatedTime
            })
            .ToListAsync();

        // 获取每个菜单的权限列表
        var menuIds = menus.Select(x => x.Id).ToList();
        var permissions = await _context.SysPermission
            .Where(x => menuIds.Contains(x.MenuId) && x.Status == ActiveStatus.正常)
            .OrderBy(x => x.Sort)
            .Select(x => new PermissionItem
            {
                Id = x.Id,
                MenuId = x.MenuId,
                Name = x.Name,
                Code = x.Code,
                Sort = x.Sort
            })
            .ToListAsync();

        var permissionGroups = permissions.GroupBy(x => x.MenuId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var menu in menus)
        {
            menu.Permissions = permissionGroups.TryGetValue(menu.Id, out var perms) ? perms : new();
        }

        // 构建树形结构
        return BuildMenuTree(menus, null);
    }

    /// <summary>
    /// 获取用户菜单（根据用户权限过滤）
    /// </summary>
    public async Task<List<AppRouteRecord>> GetUserMenuAsync()
    {
        // 获取当前用户信息
        var currentUser = await _context.SysUser
            .Where(x => x.Id == _user.Id)
            .Select(x => new { x.Id, x.IsSuper })
            .FirstAsync();

        // 构建菜单查询
        var menuQuery = _context.SysMenu.Where(m => m.Status == ActiveStatus.正常);

        List<long> userRoleIds = new();
        if (!currentUser.IsSuper)
        {
            // 获取用户角色
            userRoleIds = await _context.SysUserRole
                .Where(x => x.UserId == _user.Id)
                .Select(x => x.RoleId)
                .ToListAsync();

            // 获取角色拥有的菜单ID
            var userMenuIds = await _context.SysRoleMenu
                .Where(x => userRoleIds.Contains(x.RoleId))
                .Select(x => x.MenuId)
                .Distinct()
                .ToListAsync();

            // 递归获取所有父级菜单ID
            var allMenuIds = await GetMenuIdsWithParentsAsync(userMenuIds);
            menuQuery = menuQuery.Where(m => allMenuIds.Contains(m.Id));
        }

        // 查询菜单
        var menus = await menuQuery
            .OrderBy(m => m.Sort)
            .Select(m => new AppRouteRecord
            {
                Id = m.Id,
                ParentId = m.ParentId,
                Name = m.Code,
                Path = m.Path,
                Component = m.Component,
                Meta = new RouteMeta
                {
                    Title = m.Name,
                    Icon = m.Icon,
                    Link = m.Link,
                    IsIframe = m.IsIframe,
                    KeepAlive = m.KeepAlive,
                    IsHide = !m.IsVisible,
                    IsHideTab = m.IsHideTab,
                    Sort = m.Sort,
                    IsFullPage = m.IsFullPage,
                    ShowBadge = m.ShowBadge,
                    ShowTextBadge = m.ShowTextBadge,
                    FixedTab = m.FixedTab,
                    ActivePath = m.ActivePath,
                    Roles = string.IsNullOrEmpty(m.Roles) ? null : m.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                }
            })
            .ToListAsync();

        // 获取权限
        var menuIds = menus.Select(m => m.Id).ToList();

        List<AuthItem> permissions;
        if (currentUser.IsSuper)
        {
            permissions = await _context.SysPermission
                .Where(p => menuIds.Contains(p.MenuId) && p.Status == ActiveStatus.正常)
                .Select(x => new AuthItem
                {
                    Id = x.Id,
                    ParentId = x.MenuId,
                    Title = x.Name,
                    AuthMark = x.Code,
                    Sort = x.Sort
                })
                .ToListAsync();
        }
        else
        {
            var userPermIds = await _context.SysRolePermission
                .Where(x => userRoleIds.Contains(x.RoleId))
                .Select(x => x.PermissionId)
                .Distinct()
                .ToListAsync();

            permissions = await _context.SysPermission
                .Where(p => menuIds.Contains(p.MenuId) && userPermIds.Contains(p.Id) && p.Status == ActiveStatus.正常)
                .Select(x => new AuthItem
                {
                    Id = x.Id,
                    ParentId = x.MenuId,
                    Title = x.Name,
                    AuthMark = x.Code,
                    Sort = x.Sort
                })
                .ToListAsync();
        }

        // 将权限挂载到菜单
        var permissionGroups = permissions
            .Where(x => x.ParentId.HasValue)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        foreach (var menu in menus)
        {
            menu.Meta.AuthList = menu.Id.HasValue && permissionGroups.TryGetValue(menu.Id.Value, out var perms) ? perms : new();
        }

        // 构建树形结构
        return BuildRouteTree(menus, null);
    }

    /// <summary>
    /// 新增/更新菜单
    /// </summary>
    public async Task<UpdateMenuResponse> UpdateMenuAsync(UpdateMenuRequest request)
    {
        SysMenu? menu = null;

        if (request.Id.HasValue)
        {
            menu = await _context.SysMenu.FirstOrDefaultAsync(x => x.Id == request.Id.Value);
            if (menu == null)
                throw new BadRequestException("菜单不存在");

            // 检查编码是否重复
            if (!string.IsNullOrWhiteSpace(request.Code) &&
                await _context.SysMenu.AnyAsync(x => x.Code == request.Code && x.Id != request.Id.Value))
                throw new BadRequestException("菜单编码已存在");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new BadRequestException("菜单名称不能为空");

            if (string.IsNullOrWhiteSpace(request.Code))
                throw new BadRequestException("菜单编码不能为空");

            if (await _context.SysMenu.AnyAsync(x => x.Code == request.Code))
                throw new BadRequestException("菜单编码已存在");

            menu = new SysMenu();
            _context.Add(menu);
        }

        // 更新字段
        menu.ParentId = request.ParentId;
        menu.Name = request.Name ?? menu.Name;
        menu.Code = request.Code ?? menu.Code;
        menu.Path = request.Path ?? menu.Path;
        menu.Component = request.Component ?? menu.Component;
        menu.Icon = request.Icon ?? menu.Icon;
        menu.Sort = request.Sort ?? menu.Sort;
        menu.IsVisible = request.IsVisible ?? menu.IsVisible;
        menu.IsExternal = request.IsExternal ?? menu.IsExternal;
        menu.IsIframe = request.IsIframe ?? menu.IsIframe;
        menu.IsFullPage = request.IsFullPage ?? menu.IsFullPage;
        menu.KeepAlive = request.KeepAlive ?? menu.KeepAlive;
        menu.Link = request.Link ?? menu.Link;
        menu.Roles = request.Roles ?? menu.Roles;
        menu.ShowBadge = request.ShowBadge ?? menu.ShowBadge;
        menu.ShowTextBadge = request.ShowTextBadge ?? menu.ShowTextBadge;
        menu.FixedTab = request.FixedTab ?? menu.FixedTab;
        menu.IsHideTab = request.IsHideTab ?? menu.IsHideTab;
        menu.ActivePath = request.ActivePath ?? menu.ActivePath;
        menu.Status = request.Status ?? menu.Status;
        menu.UpdatedTime = DateTime.Now;

        await _context.SaveChangesAsync();

        return new UpdateMenuResponse { Id = menu.Id };
    }

    /// <summary>
    /// 删除菜单
    /// </summary>
    public async Task<bool> DeleteMenuAsync(long id)
    {
        var menu = await _context.SysMenu.FirstOrDefaultAsync(x => x.Id == id);
        if (menu == null)
            throw new BadRequestException("菜单不存在");

        // 检查是否有子菜单
        if (await _context.SysMenu.AnyAsync(x => x.ParentId == id))
            throw new BadRequestException("该菜单存在子菜单，无法删除");

        // 删除菜单权限
        var permissions = await _context.SysPermission.Where(x => x.MenuId == id).ToListAsync();
        if (permissions.Any())
        {
            var permIds = permissions.Select(x => x.Id).ToList();
            var rolePerms = await _context.SysRolePermission.Where(x => permIds.Contains(x.PermissionId)).ToListAsync();
            _context.RemoveRange(rolePerms);
            _context.RemoveRange(permissions);
        }

        // 删除角色菜单关联
        var roleMenus = await _context.SysRoleMenu.Where(x => x.MenuId == id).ToListAsync();
        if (roleMenus.Any())
            _context.RemoveRange(roleMenus);

        _context.Remove(menu);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 新增/更新权限
    /// </summary>
    public async Task<UpdatePermissionResponse> UpdatePermissionAsync(UpdatePermissionRequest request)
    {
        SysPermission? permission = null;

        if (request.Id.HasValue)
        {
            permission = await _context.SysPermission.FirstOrDefaultAsync(x => x.Id == request.Id.Value);
            if (permission == null)
                throw new BadRequestException("权限不存在");

            if (!string.IsNullOrWhiteSpace(request.Code) &&
                await _context.SysPermission.AnyAsync(x => x.Code == request.Code && x.Id != request.Id.Value))
                throw new BadRequestException("权限编码已存在");
        }
        else
        {
            if (!request.MenuId.HasValue)
                throw new BadRequestException("所属菜单不能为空");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new BadRequestException("权限名称不能为空");

            if (string.IsNullOrWhiteSpace(request.Code))
                throw new BadRequestException("权限编码不能为空");

            if (await _context.SysPermission.AnyAsync(x => x.Code == request.Code))
                throw new BadRequestException("权限编码已存在");

            permission = new SysPermission();
            _context.Add(permission);
        }

        permission.MenuId = request.MenuId ?? permission.MenuId;
        permission.Name = request.Name ?? permission.Name;
        permission.Code = request.Code ?? permission.Code;
        permission.Sort = request.Sort ?? permission.Sort;
        permission.Description = request.Description ?? permission.Description;
        permission.Status = request.Status ?? permission.Status;

        await _context.SaveChangesAsync();

        return new UpdatePermissionResponse { Id = permission.Id };
    }

    /// <summary>
    /// 删除权限
    /// </summary>
    public async Task<bool> DeletePermissionAsync(long id)
    {
        var permission = await _context.SysPermission.FirstOrDefaultAsync(x => x.Id == id);
        if (permission == null)
            throw new BadRequestException("权限不存在");

        // 删除角色权限关联
        var rolePerms = await _context.SysRolePermission.Where(x => x.PermissionId == id).ToListAsync();
        if (rolePerms.Any())
            _context.RemoveRange(rolePerms);

        _context.Remove(permission);
        await _context.SaveChangesAsync();

        return true;
    }

    #region 私有方法

    /// <summary>
    /// 构建菜单树
    /// </summary>
    private List<MenuTreeItem> BuildMenuTree(List<MenuTreeItem> menus, long? parentId)
    {
        return menus
            .Where(x => x.ParentId == parentId)
            .Select(x =>
            {
                x.Children = BuildMenuTree(menus, x.Id);
                return x;
            })
            .ToList();
    }

    /// <summary>
    /// 构建路由树
    /// </summary>
    private List<AppRouteRecord> BuildRouteTree(List<AppRouteRecord> routes, long? parentId)
    {
        return routes
            .Where(x => x.ParentId == parentId)
            .Select(x =>
            {
                x.Children = BuildRouteTree(routes, x.Id);
                return x;
            })
            .ToList();
    }

    /// <summary>
    /// 递归获取菜单ID及其所有父级菜单ID
    /// </summary>
    private async Task<HashSet<long>> GetMenuIdsWithParentsAsync(List<long> menuIds)
    {
        if (!menuIds.Any())
            return new HashSet<long>();

        var allMenuParentMap = await _context.SysMenu
            .Where(m => m.Status == ActiveStatus.正常)
            .Select(m => new { m.Id, m.ParentId })
            .ToDictionaryAsync(m => m.Id, m => m.ParentId);

        var result = new HashSet<long>(menuIds);
        var queue = new Queue<long>(menuIds);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            if (allMenuParentMap.TryGetValue(currentId, out var parentId) &&
                parentId.HasValue &&
                !result.Contains(parentId.Value))
            {
                result.Add(parentId.Value);
                queue.Enqueue(parentId.Value);
            }
        }

        return result;
    }

    #endregion
}

#region 请求/响应模型

/// <summary>
/// 菜单树项
/// </summary>
public class MenuTreeItem
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public int Sort { get; set; }
    public bool IsVisible { get; set; }
    public bool IsExternal { get; set; }
    public bool IsIframe { get; set; }
    public bool IsFullPage { get; set; }
    public bool KeepAlive { get; set; }
    public string? Link { get; set; }
    public string? Roles { get; set; }
    public bool ShowBadge { get; set; }
    public string? ShowTextBadge { get; set; }
    public bool FixedTab { get; set; }
    public bool IsHideTab { get; set; }
    public string? ActivePath { get; set; }
    public ActiveStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
    public List<PermissionItem> Permissions { get; set; } = new();
    public List<MenuTreeItem> Children { get; set; } = new();
}

/// <summary>
/// 权限项
/// </summary>
public class PermissionItem
{
    public long Id { get; set; }
    public long MenuId { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public int Sort { get; set; }
}

/// <summary>
/// 路由记录（前端格式）
/// </summary>
public class AppRouteRecord
{
    public long? Id { get; set; }
    public long? ParentId { get; set; }
    public string? Name { get; set; }
    public string? Path { get; set; }
    public string? Component { get; set; }
    public RouteMeta Meta { get; set; } = new();
    public List<AppRouteRecord>? Children { get; set; }
}

/// <summary>
/// 路由元数据
/// </summary>
public class RouteMeta
{
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Link { get; set; }
    public bool? IsIframe { get; set; }
    public bool? KeepAlive { get; set; }
    public bool? IsHide { get; set; }
    public bool? IsHideTab { get; set; }
    public bool? IsFullPage { get; set; }
    public bool? ShowBadge { get; set; }
    public string? ShowTextBadge { get; set; }
    public bool? FixedTab { get; set; }
    public string? ActivePath { get; set; }
    public string[]? Roles { get; set; }
    public int Sort { get; set; }
    public List<AuthItem>? AuthList { get; set; }
}

/// <summary>
/// 权限项（前端格式）
/// </summary>
public class AuthItem
{
    public long? Id { get; set; }
    public long? ParentId { get; set; }
    public string? Title { get; set; }
    public string? AuthMark { get; set; }
    public int Sort { get; set; }
}

/// <summary>
/// 新增/更新菜单请求
/// </summary>
public class UpdateMenuRequest
{
    public long? Id { get; set; }
    public long? ParentId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public int? Sort { get; set; }
    public bool? IsVisible { get; set; }
    public bool? IsExternal { get; set; }
    public bool? IsIframe { get; set; }
    public bool? IsFullPage { get; set; }
    public bool? KeepAlive { get; set; }
    public string? Link { get; set; }
    public string? Roles { get; set; }
    public bool? ShowBadge { get; set; }
    public string? ShowTextBadge { get; set; }
    public bool? FixedTab { get; set; }
    public bool? IsHideTab { get; set; }
    public string? ActivePath { get; set; }
    public ActiveStatus? Status { get; set; }
}

/// <summary>
/// 更新菜单响应
/// </summary>
public class UpdateMenuResponse
{
    public long Id { get; set; }
}

/// <summary>
/// 新增/更新权限请求
/// </summary>
public class UpdatePermissionRequest
{
    public long? Id { get; set; }
    public long? MenuId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public int? Sort { get; set; }
    public string? Description { get; set; }
    public ActiveStatus? Status { get; set; }
}

/// <summary>
/// 更新权限响应
/// </summary>
public class UpdatePermissionResponse
{
    public long Id { get; set; }
}

#endregion
