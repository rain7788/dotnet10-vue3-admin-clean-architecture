-- 还原 Demo 菜单数据
-- 将工作台从顶层菜单改为仪表盘的子菜单，并添加所有 Demo 演示菜单

-- 1. 添加仪表盘父菜单
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `status`) VALUES
(1000000000001000, NULL, '仪表盘', 'dashboard', '/dashboard', '/index/index', 'ri:pie-chart-line', 1, 1, 1);

-- 2. 更新工作台为仪表盘的子菜单
UPDATE `sys_menu` SET
  `parent_id` = 1000000000001000,
  `path` = 'console',
  `icon` = 'ri:home-smile-2-line',
  `fixed_tab` = 1
WHERE `id` = 1000000000001001;

-- 3. 添加仪表盘其他子页面
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `status`) VALUES
(1000000000001002, 1000000000001000, '分析页', 'analysis', 'analysis', '/dashboard/analysis', 'ri:align-item-bottom-line', 2, 1, 1),
(1000000000001003, 1000000000001000, '电子商务', 'ecommerce', 'ecommerce', '/dashboard/ecommerce', 'ri:bar-chart-box-line', 3, 1, 1);

-- 4. 添加模板中心
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `status`) VALUES
(1000000000003000, NULL, '模板中心', 'template', '/template', '/index/index', 'ri:apps-2-line', 2, 1, 1),
(1000000000003001, 1000000000003000, '卡片', 'cards', 'cards', '/template/cards', 'ri:wallet-line', 1, 1, 1),
(1000000000003002, 1000000000003000, '横幅', 'banners', 'banners', '/template/banners', 'ri:rectangle-line', 2, 1, 1),
(1000000000003003, 1000000000003000, '图表', 'charts', 'charts', '/template/charts', 'ri:bar-chart-box-line', 3, 1, 1),
(1000000000003004, 1000000000003000, '地图', 'map', 'map', '/template/map', 'ri:map-pin-line', 4, 1, 1),
(1000000000003005, 1000000000003000, '聊天', 'chat', 'chat', '/template/chat', 'ri:message-3-line', 5, 1, 1),
(1000000000003006, 1000000000003000, '日历', 'calendar', 'calendar', '/template/calendar', 'ri:calendar-2-line', 6, 1, 1),
(1000000000003007, 1000000000003000, '定价', 'pricing', 'pricing', '/template/pricing', 'ri:money-cny-box-line', 7, 1, 1);

-- 设置定价页全屏
UPDATE `sys_menu` SET `is_full_page` = 1 WHERE `id` = 1000000000003007;

-- 5. 添加组件中心
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `status`) VALUES
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
(1000000000004012, 1000000000004000, '礼花', 'fireworks', 'fireworks', '/widgets/fireworks', 'ri:magic-line', 12, 1, 1);

-- 设置礼花徽章
UPDATE `sys_menu` SET `show_text_badge` = 'Hot' WHERE `id` = 1000000000004012;

-- 组件总览（内嵌iframe）
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `is_iframe`, `keep_alive`, `link`, `status`) VALUES
(1000000000004013, 1000000000004000, '组件总览', 'element-ui', '/outside/iframe/elementui', '', 'ri:apps-2-line', 13, 1, 1, 0, 'https://element-plus.org/zh-CN/component/overview.html', 1);

-- 6. 添加功能示例
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `status`) VALUES
(1000000000005000, NULL, '功能示例', 'examples', '/examples', '/index/index', 'ri:sparkling-line', 4, 1, 1),
(1000000000005100, 1000000000005000, '前端权限', 'permission', 'permission', NULL, 'ri:fingerprint-line', 1, 1, 1),
(1000000000005101, 1000000000005100, '切换权限', 'switch-role', 'switch-role', '/examples/permission/switch-role', 'ri:contacts-line', 1, 1, 1),
(1000000000005102, 1000000000005100, '按钮权限演示', 'button-auth', 'button-auth', '/examples/permission/button-auth', 'ri:mouse-line', 2, 1, 1),
(1000000000005103, 1000000000005100, '超级管理员可见', 'page-visibility', 'page-visibility', '/examples/permission/page-visibility', 'ri:user-3-line', 3, 1, 1),
(1000000000005001, 1000000000005000, '标签页', 'tabs', 'tabs', '/examples/tabs', 'ri:price-tag-line', 2, 1, 1),
(1000000000005002, 1000000000005000, '基础表格', 'tables-basic', 'tables/basic', '/examples/tables/basic', 'ri:layout-grid-line', 3, 1, 1),
(1000000000005003, 1000000000005000, '高级表格', 'tables', 'tables', '/examples/tables', 'ri:table-3', 4, 1, 1),
(1000000000005004, 1000000000005000, '表单', 'forms', 'forms', '/examples/forms', 'ri:table-view', 5, 1, 1),
(1000000000005005, 1000000000005000, '搜索表单', 'search-bar', 'form/search-bar', '/examples/forms/search-bar', 'ri:table-line', 6, 1, 1),
(1000000000005006, 1000000000005000, '左右布局表格', 'tables-tree', 'tables/tree', '/examples/tables/tree', 'ri:layout-2-line', 7, 1, 1),
(1000000000005007, 1000000000005000, 'Socket 连接', 'socket-chat', 'socket-chat', '/examples/socket-chat', 'ri:shake-hands-line', 8, 1, 1);

-- 设置Socket连接徽章
UPDATE `sys_menu` SET `show_text_badge` = 'New' WHERE `id` = 1000000000005007;
-- 设置超管可见页面角色
UPDATE `sys_menu` SET `roles` = 'R_SUPER' WHERE `id` = 1000000000005103;

-- 7. 添加文章管理
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `status`) VALUES
(1000000000006000, NULL, '文章管理', 'article', '/article', '/index/index', 'ri:book-2-line', 5, 1, 1),
(1000000000006001, 1000000000006000, '文章列表', 'article-list', 'article-list', '/article/list', 'ri:article-line', 1, 1, 1),
(1000000000006002, 1000000000006000, '文章详情', 'article-detail', 'detail/:id', '/article/detail', NULL, 2, 0, 1),
(1000000000006003, 1000000000006000, '留言管理', 'comment', 'comment', '/article/comment', 'ri:mail-line', 3, 1, 1),
(1000000000006004, 1000000000006000, '文章发布', 'article-publish', 'publish', '/article/publish', 'ri:telegram-2-line', 4, 1, 1);

-- 设置文章详情激活路径
UPDATE `sys_menu` SET `active_path` = '/article/article-list' WHERE `id` = 1000000000006002;

-- 8. 添加嵌套菜单（系统管理子菜单）
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `status`) VALUES
(1000000000002100, 1000000000002000, '嵌套菜单', 'nested', 'nested', NULL, 'ri:menu-unfold-3-line', 5, 1, 1),
(1000000000002101, 1000000000002100, '菜单1', 'menu1', 'menu1', '/system/nested/menu1', 'ri:align-justify', 1, 1, 1),
(1000000000002200, 1000000000002100, '菜单2', 'menu2', 'menu2', NULL, 'ri:align-justify', 2, 1, 1),
(1000000000002201, 1000000000002200, '菜单2-1', 'menu2-1', 'menu2-1', '/system/nested/menu2', 'ri:align-justify', 1, 1, 1),
(1000000000002300, 1000000000002100, '菜单3', 'menu3', 'menu3', NULL, 'ri:align-justify', 3, 1, 1),
(1000000000002301, 1000000000002300, '菜单3-1', 'menu3-1', 'menu3-1', '/system/nested/menu3', NULL, 1, 1, 1),
(1000000000002400, 1000000000002300, '菜单3-2', 'menu3-2', 'menu3-2', NULL, NULL, 2, 1, 1),
(1000000000002401, 1000000000002400, '菜单3-2-1', 'menu3-2-1', 'menu3-2-1', '/system/nested/menu3/menu3-2', NULL, 1, 1, 1);

-- 9. 添加运维管理
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `status`) VALUES
(1000000000007000, NULL, '运维管理', 'safeguard', '/safeguard', '/index/index', 'ri:shield-check-line', 80, 1, 1),
(1000000000007001, 1000000000007000, '服务器管理', 'server', 'server', '/safeguard/server', 'ri:hard-drive-3-line', 1, 1, 1);

-- 10. 添加更新日志
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `status`) VALUES
(1000000000008001, NULL, '更新日志', 'change-log', '/change/log', '/change/log', 'ri:gamepad-line', 95, 1, 1);

-- 11. 为超管分配新增菜单
INSERT INTO `sys_role_menu` (`role_id`, `menu_id`) VALUES
-- 仪表盘父菜单及新子菜单
(1000000000000001, 1000000000001000), (1000000000000001, 1000000000001002), (1000000000000001, 1000000000001003),
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
-- 嵌套菜单
(1000000000000001, 1000000000002100), (1000000000000001, 1000000000002101), (1000000000000001, 1000000000002200), (1000000000000001, 1000000000002201),
(1000000000000001, 1000000000002300), (1000000000000001, 1000000000002301), (1000000000000001, 1000000000002400), (1000000000000001, 1000000000002401),
-- 运维管理
(1000000000000001, 1000000000007000), (1000000000000001, 1000000000007001),
-- 更新日志
(1000000000000001, 1000000000008001);
