# 数据库表结构

Art Admin 使用 **MySQL 8.0**，核心包含 9 张表。主键使用 `BIGINT` 雪花 ID（Token 表除外）。

## 表一览

| 表名 | 说明 | 主键策略 |
| --- | --- | --- |
| `sys_user` | 系统用户 | 雪花 ID |
| `sys_role` | 系统角色 | 雪花 ID |
| `sys_menu` | 系统菜单 | 雪花 ID |
| `sys_permission` | 权限标识 | 雪花 ID |
| `sys_user_role` | 用户-角色关联 | 复合主键 |
| `sys_role_menu` | 角色-菜单关联 | 复合主键 |
| `sys_role_permission` | 角色-权限关联 | 复合主键 |
| `token_access` | 访问令牌 | 自增 ID |
| `token_refresh` | 刷新令牌 | 自增 ID |

## 核心表结构

### sys_user（系统用户）

```sql
CREATE TABLE `sys_user` (
  `id` bigint NOT NULL COMMENT '主键ID（雪花ID）',
  `username` varchar(50) NOT NULL COMMENT '用户名',
  `password` varchar(1024) NOT NULL COMMENT '密码（加密存储）',
  `real_name` varchar(50) DEFAULT NULL COMMENT '真实姓名',
  `is_super` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否超级管理员',
  `phone` varchar(20) DEFAULT NULL COMMENT '手机号',
  `avatar` varchar(200) DEFAULT NULL COMMENT '头像',
  `status` int NOT NULL DEFAULT 1 COMMENT '状态：0-不可用，1-正常',
  `last_login_time` datetime(6) DEFAULT NULL COMMENT '最后登录时间',
  `last_active_time` datetime(6) DEFAULT NULL COMMENT '最近活跃时间',
  `created_time` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`),
  UNIQUE KEY `idx_sys_user_username` (`username`)
) ENGINE=InnoDB COMMENT='系统用户表';
```

### sys_menu（菜单 + RBAC 权限）

```sql
CREATE TABLE `sys_menu` (
  `id` BIGINT NOT NULL COMMENT '主键ID（雪花ID）',
  `parent_id` BIGINT NULL COMMENT '父级菜单ID',
  `name` VARCHAR(50) NOT NULL COMMENT '菜单名称',
  `code` VARCHAR(50) NOT NULL COMMENT '菜单编码/权限标识',
  `path` VARCHAR(200) NULL COMMENT '路由地址',
  `component` VARCHAR(200) NULL COMMENT '组件路径',
  `icon` VARCHAR(100) NULL COMMENT '图标',
  `sort` INT NOT NULL DEFAULT 0 COMMENT '排序',
  `is_visible` TINYINT(1) NOT NULL DEFAULT 1 COMMENT '是否可见',
  `is_external` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否外链',
  `is_iframe` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否内嵌iframe',
  `is_full_page` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否全屏页面',
  `keep_alive` TINYINT(1) NOT NULL DEFAULT 1 COMMENT '是否缓存',
  `status` INT NOT NULL DEFAULT 1 COMMENT '状态',
  `created_time` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`id`)
) ENGINE=InnoDB COMMENT='系统菜单表';
```

### RBAC 关联表

```sql
-- 用户-角色（多对多）
CREATE TABLE `sys_user_role` (
  `user_id` BIGINT NOT NULL,
  `role_id` BIGINT NOT NULL,
  PRIMARY KEY (`user_id`, `role_id`)
);

-- 角色-菜单（多对多）
CREATE TABLE `sys_role_menu` (
  `role_id` BIGINT NOT NULL,
  `menu_id` BIGINT NOT NULL,
  PRIMARY KEY (`role_id`, `menu_id`)
);

-- 角色-权限（多对多）
CREATE TABLE `sys_role_permission` (
  `role_id` BIGINT NOT NULL,
  `permission_id` BIGINT NOT NULL,
  PRIMARY KEY (`role_id`, `permission_id`)
);
```

## 设计原则

### 无外键约束

::: warning
数据库中**不使用外键约束**。关联关系仅在 EF Core 通过 `[ForeignKey]` 声明导航属性，MySQL 表中不存在实际 FK。

好处：
- 迁移更灵活，不受外键顺序限制
- 删除/更新不触发级联
- 性能更好（减少锁竞争）
:::

### 雪花 ID

所有业务实体主键使用 `BIGINT` + 雪花 ID（`IdGen.NextId()`），Token 表使用自增 ID。

### 时间精度

所有时间字段使用 `datetime(6)` 微秒精度。

### Snake Case 命名

EF Core 配置了 snake_case 命名策略，C# 的 `PascalCase` 属性自动映射为 `snake_case` 列名。

## 文件位置

```
database/
├── schemas/
│   └── 01_core_tables.sql    # 完整表结构
├── seeds/
│   └── 01_sys_user.sql       # 初始数据
└── migrations/
    └── yyyyMMdd_desc.sql     # 增量迁移
```
