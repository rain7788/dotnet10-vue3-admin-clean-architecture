-- ----------------------------
-- sys_user 增加最近活跃时间字段
-- ----------------------------
ALTER TABLE `sys_user`
  ADD COLUMN `last_active_time` datetime(6) DEFAULT NULL COMMENT '最近活跃时间' AFTER `last_login_time`;
