# 数据库迁移

Art Admin 使用**纯 SQL 文件**管理数据库变更，不使用 EF Core Migrations。

## 三同步原则

每次数据库变更需同步更新三处：

```
database/
├── schemas/01_core_tables.sql    # 1. 完整表结构（保持最新）
├── seeds/01_sys_user.sql         # 2. 初始数据（如有变化）
└── migrations/yyyyMMdd_desc.sql  # 3. 增量迁移脚本
```

## 迁移文件命名

格式：`yyyyMMdd_描述.sql`

```
migrations/
├── 20260117_snowflake_id.sql
├── 20260217_add_delay_queue_demo_menu.sql
├── 20260217_add_distributed_lock_demo_menu.sql
├── 20260217_add_sys_user_last_active_time.sql
└── 20260217_restore_demo_menus.sql
```

## 迁移脚本示例

### 新增菜单

```sql
-- 新增延迟队列 Demo 菜单
INSERT INTO `sys_menu` (
  `id`, `parent_id`, `name`, `code`, `path`,
  `component`, `icon`, `sort`, `is_visible`, `status`
)
VALUES (
  1891234567890123459, 1891234567890123456,
  '延迟队列', 'DelayQueue', '/demo/delay-queue',
  '/examples/delay-queue/index', 'ri:timer-line',
  3, 1, 1
);
```

### 新增字段

```sql
-- 为 sys_user 表新增最近活跃时间字段
ALTER TABLE `sys_user`
ADD COLUMN `last_active_time` datetime(6) DEFAULT NULL
COMMENT '最近活跃时间' AFTER `last_login_time`;
```

## 新增页面必加菜单

::: warning 重要
新增页面时，**必须**在 `database/migrations/` 中插入 `sys_menu` 菜单记录。前端使用后端路由模式，菜单数据来自数据库。
:::

```sql
-- 新增页面菜单
INSERT INTO `sys_menu` (
  `id`, `parent_id`, `name`, `code`, `path`, `component`,
  `icon`, `sort`, `is_visible`, `status`
)
VALUES (
  雪花ID, 父菜单ID, '页面名称', 'PageCode',
  '/module/page-name', '/module/page-name/index',
  'icon-name', 排序, 1, 1
);
```

## 执行迁移

```bash
# 手动执行 SQL 文件
mysql -h localhost -P 3306 -u root -p aaaaaa art < database/migrations/20260217_xxx.sql
```

## 注意事项

1. **无外键约束** — 所有迁移脚本中不要添加 `FOREIGN KEY` 约束
2. **幂等性** — 建议使用 `IF NOT EXISTS` 或 `INSERT IGNORE` 确保可重复执行
3. **Schema 同步** — 每次写迁移脚本后，务必同步更新 `schemas/01_core_tables.sql`
4. **Seeds 同步** — 如果修改了初始数据，同步更新 `seeds/` 中的对应文件
