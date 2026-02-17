-- Art Admin 初始化数据
-- 默认管理员账号：admin / 123456
-- 注意：所有 ID 现在使用 BIGINT 类型（雪花ID格式）

-- ----------------------------
-- 初始化超级管理员用户
-- 密码：123456（使用 SHA256 + Base64 加密）
-- ----------------------------
INSERT INTO `sys_user` (`id`, `username`, `password`, `real_name`, `is_super`, `status`, `created_time`) 
VALUES (1000000000000001, 'admin', 'jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=', '超级管理员', 1, 1, NOW(6));


-- ----------------------------
-- RBAC 权限管理初始数据
-- ----------------------------

-- 初始化角色数据
INSERT INTO `sys_role` (`id`, `name`, `code`, `description`, `status`) VALUES
(1000000000000001, '超级管理员', 'SUPER_ADMIN', '拥有系统所有权限', 1),
(1000000000000002, '普通管理员', 'ADMIN', '拥有基本管理权限', 1),
(1000000000000003, '普通用户', 'USER', '普通用户角色', 1);

-- 初始化菜单数据
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `status`) VALUES
-- 仪表盘
(1000000000001000, NULL, '仪表盘', 'dashboard', '/dashboard', '/index/index', 'ri:pie-chart-line', 1, 1, 1),
(1000000000001001, 1000000000001000, '工作台', 'console', 'console', '/dashboard/console', 'ri:home-smile-2-line', 1, 1, 1),
(1000000000001002, 1000000000001000, '分析页', 'analysis', 'analysis', '/dashboard/analysis', 'ri:align-item-bottom-line', 2, 1, 1),
(1000000000001003, 1000000000001000, '电子商务', 'ecommerce', 'ecommerce', '/dashboard/ecommerce', 'ri:bar-chart-box-line', 3, 1, 1),
-- 模板中心
(1000000000003000, NULL, '模板中心', 'template', '/template', '/index/index', 'ri:apps-2-line', 2, 1, 1),
(1000000000003001, 1000000000003000, '卡片', 'cards', 'cards', '/template/cards', 'ri:wallet-line', 1, 1, 1),
(1000000000003002, 1000000000003000, '横幅', 'banners', 'banners', '/template/banners', 'ri:rectangle-line', 2, 1, 1),
(1000000000003003, 1000000000003000, '图表', 'charts', 'charts', '/template/charts', 'ri:bar-chart-box-line', 3, 1, 1),
(1000000000003004, 1000000000003000, '地图', 'map', 'map', '/template/map', 'ri:map-pin-line', 4, 1, 1),
(1000000000003005, 1000000000003000, '聊天', 'chat', 'chat', '/template/chat', 'ri:message-3-line', 5, 1, 1),
(1000000000003006, 1000000000003000, '日历', 'calendar', 'calendar', '/template/calendar', 'ri:calendar-2-line', 6, 1, 1),
(1000000000003007, 1000000000003000, '定价', 'pricing', 'pricing', '/template/pricing', 'ri:money-cny-box-line', 7, 1, 1),
-- 组件中心
(1000000000004000, NULL, '组件中心', 'widgets', '/widgets', '/index/index', 'ri:apps-2-add-line', 3, 1, 1),
(1000000000004001, 1000000000004000, '图标', 'icon', 'icon', '/widgets/icon', 'ri:palette-line', 1, 1, 1),
(1000000000004002, 1000000000004000, '图像裁剪', 'image-crop', 'image-crop', '/widgets/image-crop', 'ri:screenshot-line', 2, 1, 1),
(1000000000004003, 1000000000004000, 'Excel', 'excel', 'excel', '/widgets/excel', 'ri:download-2-line', 3, 1, 1),
(1000000000004004, 1000000000004000, '视频播放器', 'video', 'video', '/widgets/video', 'ri:vidicon-line', 4, 1, 1),
(1000000000004005, 1000000000004000, '数字滚动', 'count-to', 'count-to', '/widgets/count-to', 'ri:anthropic-line', 5, 1, 1),
(1000000000004006, 1000000000004000, '富文本编辑器', 'wang-editor', 'wang-editor', '/widgets/wang-editor', 'ri:t-box-line', 6, 1, 1),
(1000000000004007, 1000000000004000, '水印', 'watermark', 'watermark', '/widgets/watermark', 'ri:water-flash-line', 7, 1, 1),
(1000000000004008, 1000000000004000, '右键菜单', 'context-menu', 'context-menu', '/widgets/context-menu', 'ri:menu-2-line', 8, 1, 1),
(1000000000004009, 1000000000004000, '二维码', 'qrcode', 'qrcode', '/widgets/qrcode', 'ri:qr-code-line', 9, 1, 1),
(1000000000004010, 1000000000004000, '拖拽', 'drag', 'drag', '/widgets/drag', 'ri:drag-move-fill', 10, 1, 1),
(1000000000004011, 1000000000004000, '文字滚动', 'text-scroll', 'text-scroll', '/widgets/text-scroll', 'ri:input-method-line', 11, 1, 1),
(1000000000004012, 1000000000004000, '礼花', 'fireworks', 'fireworks', '/widgets/fireworks', 'ri:magic-line', 12, 1, 1),
-- 功能示例
(1000000000005000, NULL, '功能示例', 'examples', '/examples', '/index/index', 'ri:sparkling-line', 4, 1, 1),
(1000000000005001, 1000000000005000, '标签页', 'tabs', 'tabs', '/examples/tabs', 'ri:price-tag-line', 1, 1, 1),
(1000000000005002, 1000000000005000, '基础表格', 'tables-basic', 'tables/basic', '/examples/tables/basic', 'ri:layout-grid-line', 2, 1, 1),
(1000000000005003, 1000000000005000, '高级表格', 'tables', 'tables', '/examples/tables', 'ri:table-3', 3, 1, 1),
(1000000000005004, 1000000000005000, '表单', 'forms', 'forms', '/examples/forms', 'ri:table-view', 4, 1, 1),
(1000000000005005, 1000000000005000, '搜索表单', 'search-bar', 'form/search-bar', '/examples/forms/search-bar', 'ri:table-line', 5, 1, 1),
(1000000000005006, 1000000000005000, '左右布局表格', 'tables-tree', 'tables/tree', '/examples/tables/tree', 'ri:layout-2-line', 6, 1, 1),
(1000000000005007, 1000000000005000, 'Socket 连接', 'socket-chat', 'socket-chat', '/examples/socket-chat', 'ri:shake-hands-line', 7, 1, 1),
-- 文章管理
(1000000000006000, NULL, '文章管理', 'article', '/article', '/index/index', 'ri:book-2-line', 5, 1, 1),
(1000000000006001, 1000000000006000, '文章列表', 'article-list', 'article-list', '/article/list', 'ri:article-line', 1, 1, 1),
(1000000000006002, 1000000000006000, '文章详情', 'article-detail', 'detail/:id', '/article/detail', NULL, 2, 0, 1),
(1000000000006003, 1000000000006000, '留言管理', 'comment', 'comment', '/article/comment', 'ri:mail-line', 3, 1, 1),
(1000000000006004, 1000000000006000, '文章发布', 'article-publish', 'publish', '/article/publish', 'ri:telegram-2-line', 4, 1, 1),
-- 系统管理
(1000000000002000, NULL, '系统管理', 'system', '/system', '/index/index', 'ri:settings-line', 90, 1, 1),
(1000000000002001, 1000000000002000, '用户管理', 'user', 'user', '/system/user', 'ri:user-line', 1, 1, 1),
(1000000000002002, 1000000000002000, '角色管理', 'role', 'role', '/system/role', 'ri:user-settings-line', 2, 1, 1),
(1000000000002003, 1000000000002000, '菜单管理', 'menu', 'menu', '/system/menu', 'ri:menu-line', 3, 1, 1),
(1000000000002004, 1000000000002000, '系统日志', 'log', 'log', '/system/log', 'ri:history-line', 4, 1, 1),
(1000000000002100, 1000000000002000, '嵌套菜单', 'nested', 'nested', NULL, 'ri:menu-unfold-3-line', 5, 1, 1),
(1000000000002101, 1000000000002100, '菜单1', 'menu1', 'menu1', '/system/nested/menu1', 'ri:align-justify', 1, 1, 1),
(1000000000002200, 1000000000002100, '菜单2', 'menu2', 'menu2', NULL, 'ri:align-justify', 2, 1, 1),
(1000000000002201, 1000000000002200, '菜单2-1', 'menu2-1', 'menu2-1', '/system/nested/menu2', 'ri:align-justify', 1, 1, 1),
(1000000000002300, 1000000000002100, '菜单3', 'menu3', 'menu3', NULL, 'ri:align-justify', 3, 1, 1),
(1000000000002301, 1000000000002300, '菜单3-1', 'menu3-1', 'menu3-1', '/system/nested/menu3', NULL, 1, 1, 1),
(1000000000002400, 1000000000002300, '菜单3-2', 'menu3-2', 'menu3-2', NULL, NULL, 2, 1, 1),
(1000000000002401, 1000000000002400, '菜单3-2-1', 'menu3-2-1', 'menu3-2-1', '/system/nested/menu3/menu3-2', NULL, 1, 1, 1),
-- 运维管理
(1000000000007000, NULL, '运维管理', 'safeguard', '/safeguard', '/index/index', 'ri:shield-check-line', 80, 1, 1),
(1000000000007001, 1000000000007000, '服务器管理', 'server', 'server', '/safeguard/server', 'ri:hard-drive-3-line', 1, 1, 1),
(1891234567890123456, NULL, '后端示例', 'Demo', '/demo', '', 'ri:flask-line', 999, 1, 1, 1),
(1891234567890123457, 1891234567890123456, '消息队列', 'MessageQueue', '/demo/message-queue', '/examples/message-queue/index', 'ri:mail-send-line', 1, 1, 1, 1);

-- 设置工作台固定标签
UPDATE `sys_menu` SET `fixed_tab` = 1 WHERE `id` = 1000000000001001;
-- 设置定价页全屏
UPDATE `sys_menu` SET `is_full_page` = 1 WHERE `id` = 1000000000003007;
-- 设置文章详情激活路径
UPDATE `sys_menu` SET `active_path` = '/article/article-list' WHERE `id` = 1000000000006002;
-- 设置礼花徽章
UPDATE `sys_menu` SET `show_text_badge` = 'Hot' WHERE `id` = 1000000000004012;
-- 设置Socket连接徽章
UPDATE `sys_menu` SET `show_text_badge` = 'New' WHERE `id` = 1000000000005007;
-- 设置超管可见页面角色
UPDATE `sys_menu` SET `roles` = 'R_SUPER' WHERE `id` = 1000000000005103;

-- 组件总览（内嵌iframe）
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `is_iframe`, `keep_alive`, `link`, `status`) VALUES
(1000000000004013, 1000000000004000, '组件总览', 'element-ui', '/outside/iframe/elementui', '', 'ri:apps-2-line', 13, 1, 1, 0, 'https://element-plus.org/zh-CN/component/overview.html', 1);

-- 初始化权限数据
INSERT INTO `sys_permission` (`id`, `menu_id`, `name`, `code`) VALUES
(1000000000003001, 1000000000002001, '新增', 'system:user:add'),
(1000000000003002, 1000000000002001, '编辑', 'system:user:edit'),
(1000000000003003, 1000000000002001, '删除', 'system:user:delete'),
(1000000000003004, 1000000000002002, '新增', 'system:role:add'),
(1000000000003005, 1000000000002002, '编辑', 'system:role:edit'),
(1000000000003006, 1000000000002002, '删除', 'system:role:delete'),
(1000000000003007, 1000000000002003, '新增', 'system:menu:add'),
(1000000000003008, 1000000000002003, '编辑', 'system:menu:edit'),
(1000000000003009, 1000000000002003, '删除', 'system:menu:delete');

-- 为用户(admin)分配超管角色
INSERT INTO `sys_user_role` (`user_id`, `role_id`) VALUES (1000000000000001, 1000000000000001);

-- 为超管分配所有菜单
INSERT INTO `sys_role_menu` (`role_id`, `menu_id`) VALUES
-- 仪表盘
(1000000000000001, 1000000000001000), (1000000000000001, 1000000000001001), (1000000000000001, 1000000000001002), (1000000000000001, 1000000000001003),
-- 模板中心
(1000000000000001, 1000000000003000), (1000000000000001, 1000000000003001), (1000000000000001, 1000000000003002), (1000000000000001, 1000000000003003),
(1000000000000001, 1000000000003004), (1000000000000001, 1000000000003005), (1000000000000001, 1000000000003006), (1000000000000001, 1000000000003007),
-- 组件中心
(1000000000000001, 1000000000004000), (1000000000000001, 1000000000004001), (1000000000000001, 1000000000004002), (1000000000000001, 1000000000004003),
(1000000000000001, 1000000000004004), (1000000000000001, 1000000000004005), (1000000000000001, 1000000000004006), (1000000000000001, 1000000000004007),
(1000000000000001, 1000000000004008), (1000000000000001, 1000000000004009), (1000000000000001, 1000000000004010), (1000000000000001, 1000000000004011),
(1000000000000001, 1000000000004012), (1000000000000001, 1000000000004013),
-- 功能示例
(1000000000000001, 1000000000005000), (1000000000000001, 1000000000005100), (1000000000000001, 1000000000005101), (1000000000000001, 1000000000005102),
(1000000000000001, 1000000000005103), (1000000000000001, 1000000000005001), (1000000000000001, 1000000000005002), (1000000000000001, 1000000000005003),
(1000000000000001, 1000000000005004), (1000000000000001, 1000000000005005), (1000000000000001, 1000000000005006), (1000000000000001, 1000000000005007),
-- 文章管理
(1000000000000001, 1000000000006000), (1000000000000001, 1000000000006001), (1000000000000001, 1000000000006002), (1000000000000001, 1000000000006003), (1000000000000001, 1000000000006004),
-- 系统管理
(1000000000000001, 1000000000002000), (1000000000000001, 1000000000002001), (1000000000000001, 1000000000002002), (1000000000000001, 1000000000002003), (1000000000000001, 1000000000002004),
(1000000000000001, 1000000000002100), (1000000000000001, 1000000000002101), (1000000000000001, 1000000000002200), (1000000000000001, 1000000000002201),
(1000000000000001, 1000000000002300), (1000000000000001, 1000000000002301), (1000000000000001, 1000000000002400), (1000000000000001, 1000000000002401),
-- 运维管理
(1000000000000001, 1000000000007000), (1000000000000001, 1000000000007001),
-- Demo 示例
(1000000000000001, 1891234567890123456), (1000000000000001, 1891234567890123457);

-- 为超管分配所有权限
INSERT INTO `sys_role_permission` (`role_id`, `permission_id`) VALUES
(1000000000000001, 1000000000003001), (1000000000000001, 1000000000003002), (1000000000000001, 1000000000003003),
(1000000000000001, 1000000000003004), (1000000000000001, 1000000000003005), (1000000000000001, 1000000000003006),
(1000000000000001, 1000000000003007), (1000000000000001, 1000000000003008), (1000000000000001, 1000000000003009);
