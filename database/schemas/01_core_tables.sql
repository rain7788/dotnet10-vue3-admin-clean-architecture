-- Art Admin 数据库表结构定义
-- 包含：Token、系统用户等核心表
-- 主键使用 BIGINT 类型存储雪花ID

-- ----------------------------
-- 刷新令牌表（保持自增主键）
-- ----------------------------
DROP TABLE IF EXISTS `token_refresh`;
CREATE TABLE `token_refresh` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `user_id` bigint NOT NULL COMMENT '用户ID',
  `type` int DEFAULT NULL COMMENT '类型：0-无，1-玩家端，9-平台端',
  `client` int DEFAULT NULL COMMENT '客户端类型：0-小程序，1-H5，2-App，99-未知',
  `token` varchar(50) NOT NULL COMMENT '刷新令牌',
  `expiration_time` datetime(6) NOT NULL COMMENT '过期时间',
  `created_time` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE KEY `idx_token_refresh_token` (`token`),
  KEY `idx_token_refresh_user_id` (`user_id`),
  KEY `idx_token_refresh_expiration_time` (`expiration_time` DESC)
) ENGINE=InnoDB COMMENT='刷新令牌';


-- ----------------------------
-- 访问令牌表（保持自增主键）
-- ----------------------------
DROP TABLE IF EXISTS `token_access`;
CREATE TABLE `token_access` (
  `id` bigint NOT NULL AUTO_INCREMENT,
  `refresh_token_id` bigint DEFAULT NULL COMMENT '关联的刷新令牌ID',
  `user_id` bigint NOT NULL COMMENT '用户ID',
  `type` int DEFAULT NULL COMMENT '类型：0-无，1-玩家端，9-平台端',
  `client` int DEFAULT NULL COMMENT '客户端类型：0-小程序，1-H5，2-App，99-未知',
  `token` varchar(64) NOT NULL COMMENT '访问令牌',
  `expiration_time` datetime(6) NOT NULL COMMENT '过期时间',
  `created_time` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
  PRIMARY KEY (`id`),
  UNIQUE KEY `idx_token_access_token` (`token`),
  KEY `idx_token_access_user_id` (`user_id`),
  KEY `idx_token_access_expiration_time` (`expiration_time` DESC),
  KEY `idx_token_access_refresh_token_id` (`refresh_token_id`)
) ENGINE=InnoDB COMMENT='访问令牌';


-- ----------------------------
-- 系统用户表（后台管理员）- 雪花ID主键
-- ----------------------------
DROP TABLE IF EXISTS `sys_user`;
CREATE TABLE `sys_user` (
  `id` bigint NOT NULL COMMENT '主键ID（雪花ID）',
  `username` varchar(50) NOT NULL COMMENT '用户名',
  `password` varchar(1024) NOT NULL COMMENT '密码（加密存储）',
  `real_name` varchar(50) DEFAULT NULL COMMENT '真实姓名',
  `is_super` tinyint(1) NOT NULL DEFAULT 0 COMMENT '是否超级管理员 0:否 1:是',
  `phone` varchar(20) DEFAULT NULL COMMENT '手机号',
  `avatar` varchar(200) DEFAULT NULL COMMENT '头像',
  `status` int NOT NULL DEFAULT 1 COMMENT '状态：0-不可用，1-正常',
  `last_login_time` datetime(6) DEFAULT NULL COMMENT '最后登录时间',
  `created_time` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
  PRIMARY KEY (`id`),
  UNIQUE KEY `idx_sys_user_username` (`username`),
  KEY `idx_sys_user_created_time` (`created_time` DESC)
) ENGINE=InnoDB COMMENT='系统用户表';


  -- ----------------------------
  -- RBAC 权限管理表结构
  -- 角色、菜单、权限及其关联表
  -- 所有主键使用 BIGINT 存储雪花ID
  -- ----------------------------

  -- 1. 角色表 - 雪花ID主键
  DROP TABLE IF EXISTS `sys_role`;
  CREATE TABLE `sys_role` (
    `id` BIGINT NOT NULL COMMENT '主键ID（雪花ID）',
    `name` VARCHAR(50) NOT NULL COMMENT '角色名称',
    `code` VARCHAR(50) NOT NULL COMMENT '角色编码',
    `description` VARCHAR(200) NULL COMMENT '角色描述',
    `status` INT NOT NULL DEFAULT 1 COMMENT '状态: 0=不可用, 1=正常',
    `created_time` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    `updated_time` DATETIME(6) NULL COMMENT '更新时间',
    PRIMARY KEY (`id`),
    INDEX `ix_sys_role_code` (`code`)
  ) ENGINE=InnoDB COMMENT='系统角色表';

  -- 2. 菜单表 - 雪花ID主键
  DROP TABLE IF EXISTS `sys_menu`;
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
    `link` VARCHAR(500) NULL COMMENT '外部链接',
    `roles` VARCHAR(500) NULL COMMENT '角色权限标识，多个用逗号分隔',
    `show_badge` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '显示徽章',
    `show_text_badge` VARCHAR(50) NULL COMMENT '文本徽章，如New、Hot',
    `fixed_tab` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '固定标签',
    `is_hide_tab` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '标签隐藏',
    `active_path` VARCHAR(200) NULL COMMENT '激活路径，用于详情页高亮父级菜单',
    `status` INT NOT NULL DEFAULT 1 COMMENT '状态: 0=不可用, 1=正常',
    `created_time` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    `updated_time` DATETIME(6) NULL COMMENT '更新时间',
    PRIMARY KEY (`id`),
    INDEX `ix_sys_menu_parent_id` (`parent_id`)
  ) ENGINE=InnoDB COMMENT='系统菜单表';

  -- 3. 权限表 - 雪花ID主键
  DROP TABLE IF EXISTS `sys_permission`;
  CREATE TABLE `sys_permission` (
    `id` BIGINT NOT NULL COMMENT '主键ID（雪花ID）',
    `menu_id` BIGINT NOT NULL COMMENT '所属菜单ID',
    `name` VARCHAR(50) NOT NULL COMMENT '权限名称',
    `code` VARCHAR(100) NOT NULL COMMENT '权限标识',
    `sort` INT NOT NULL DEFAULT 0 COMMENT '排序',
    `description` VARCHAR(200) NULL COMMENT '权限描述',
    `status` INT NOT NULL DEFAULT 1 COMMENT '状态: 0=不可用, 1=正常',
    `created_time` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    PRIMARY KEY (`id`),
    INDEX `ix_sys_permission_menu_id` (`menu_id`),
    INDEX `ix_sys_permission_code` (`code`)
  ) ENGINE=InnoDB COMMENT='系统权限表';

  -- 4. 用户角色关联表 - 复合主键
  DROP TABLE IF EXISTS `sys_user_role`;
  CREATE TABLE `sys_user_role` (
    `user_id` BIGINT NOT NULL COMMENT '用户ID',
    `role_id` BIGINT NOT NULL COMMENT '角色ID',
    `created_time` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    PRIMARY KEY (`user_id`, `role_id`),
    INDEX `ix_sys_user_role_role_id` (`role_id`)
  ) ENGINE=InnoDB COMMENT='用户角色关联表';

  -- 5. 角色菜单关联表 - 复合主键
  DROP TABLE IF EXISTS `sys_role_menu`;
  CREATE TABLE `sys_role_menu` (
    `role_id` BIGINT NOT NULL COMMENT '角色ID',
    `menu_id` BIGINT NOT NULL COMMENT '菜单ID',
    `created_time` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    PRIMARY KEY (`role_id`, `menu_id`),
    INDEX `ix_sys_role_menu_menu_id` (`menu_id`)
  ) ENGINE=InnoDB COMMENT='角色菜单关联表';

  -- 6. 角色权限关联表 - 复合主键
  DROP TABLE IF EXISTS `sys_role_permission`;
  CREATE TABLE `sys_role_permission` (
    `role_id` BIGINT NOT NULL COMMENT '角色ID',
    `permission_id` BIGINT NOT NULL COMMENT '权限ID',
    `created_time` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    PRIMARY KEY (`role_id`, `permission_id`),
    INDEX `ix_sys_role_permission_permission_id` (`permission_id`)
  ) ENGINE=InnoDB COMMENT='角色权限关联表';
