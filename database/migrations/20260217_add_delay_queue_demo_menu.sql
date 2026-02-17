-- 新增延迟队列 Demo 菜单
INSERT INTO `sys_menu` (`id`, `parent_id`, `name`, `code`, `path`, `component`, `icon`, `sort`, `is_visible`, `status`)
VALUES (1891234567890123459, 1891234567890123456, '延迟队列', 'DelayQueue', '/demo/delay-queue', '/examples/delay-queue/index', 'ri:timer-line', 3, 1, 1);
