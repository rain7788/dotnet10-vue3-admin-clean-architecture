# 实体与导航属性

## 实体基类

所有实体继承 `EntityBase`（或 `EntityBaseWithUpdate`），自动获得雪花 ID 和创建时间：

```csharp
public abstract class EntityBase
{
    [Key]
    public long Id { get; set; } = IdGen.NextId();
    public DateTime CreatedTime { get; set; } = DateTime.Now;
}

public abstract class EntityBaseWithUpdate : EntityBase
{
    public DateTime? UpdatedTime { get; set; }
}
```

## 实体定义

```csharp
[Table("sys_user")]
public class SysUser : EntityBase
{
    [MaxLength(50)]
    public string Username { get; set; } = default!;

    [MaxLength(1024)]
    public string Password { get; set; } = default!;

    [MaxLength(50)]
    public string? RealName { get; set; }

    public bool IsSuper { get; set; } = false;

    public ActiveStatus Status { get; set; } = ActiveStatus.正常;

    public DateTime? LastLoginTime { get; set; }
}
```

关键注解：
- `[Table("表名")]` — 映射数据库表名
- `[MaxLength(n)]` — 字符串长度约束
- `[Key]` — 主键标注（基类已包含）

## 导航属性与 ForeignKey

Art Admin **数据库无外键约束**（SQL 中不创建 FK），但在实体中使用 `[ForeignKey]` 注解来定义导航属性关系。

### 关联表示例

```csharp
[Table("sys_user_role")]
public class SysUserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    // 导航属性 — 通过 ForeignKey 关联
    [ForeignKey(nameof(UserId))]
    public SysUser SysUser { get; set; } = default!;

    [ForeignKey(nameof(RoleId))]
    public SysRole SysRole { get; set; } = default!;
}
```

### Token 关联示例

```csharp
[Table("token_access")]
public class TokenAccess
{
    [Key]
    public long Id { get; set; }

    public long RefreshTokenId { get; set; }
    public long UserId { get; set; }
    public TokenType Type { get; set; }
    public string Token { get; set; } = default!;
    public DateTime ExpirationTime { get; set; }

    // 导航属性 — 关联刷新令牌
    [ForeignKey(nameof(RefreshTokenId))]
    public TokenRefresh TokenRefresh { get; set; } = default!;
}
```

## 复合主键

关联表使用复合主键，在 `DbContext.OnModelCreating` 中配置：

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<SysUserRole>()
        .HasKey(x => new { x.UserId, x.RoleId });

    modelBuilder.Entity<SysRoleMenu>()
        .HasKey(x => new { x.RoleId, x.MenuId });

    modelBuilder.Entity<SysRolePermission>()
        .HasKey(x => new { x.RoleId, x.PermissionId });
}
```

## DbContext 注册

使用 `DbContextPool` 高性能连接池 + Snake Case 命名约定：

```csharp
services.AddDbContextPool<ArtDbContext>((sp, options) =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)))
        .UseSnakeCaseNamingConvention()
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));
```

Worker 中使用 `IDbContextFactory`（因为 Worker 不在请求作用域内）：

```csharp
services.AddDbContextFactory<ArtDbContext>((sp, options) =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)))
        .UseSnakeCaseNamingConvention());
```

## 实体基类选择指南

| 基类 | 包含字段 | 适用场景 |
| --- | --- | --- |
| `EntityBase` | Id + CreatedTime | 大多数业务实体 |
| `EntityBaseWithUpdate` | + UpdatedTime | 需要记录更新时间 |
| `EntityBase<TKey>` | 自定义主键类型 + CreatedTime | 如 `string`、`Guid` 主键 |
| 无基类 | 完全自定义 | 关联表、特殊表 |
