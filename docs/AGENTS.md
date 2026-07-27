# Documentation Instructions

本文件适用于 `docs/`。同时遵循仓库根目录 `AGENTS.md`。

## 内容约定

- 文档基于 VitePress；内容文件位于 `docs/zh/` 和 `docs/en/`。
- 修改已有双语主题时同步更新中英文对应文件，保持章节结构、示例和配置语义一致。
- 新增用户可见功能、API、配置项或部署行为时，更新相关指南，不只修改 README 或代码注释。
- 命令、路径、类型和配置必须从仓库当前实现核实；不要保留已经删除的文件或过时调用方式。
- 示例应可直接执行且不得包含真实密码、Token、服务器地址等生产凭据。

## 结构与链接

- 沿用现有 `guide/`、`backend/`、`frontend/`、`database/`、`deployment/` 分类。
- 新增或移动页面时同步检查 VitePress 导航、侧边栏和所有站内链接。
- 图片等静态资源放在 `docs/public/`，使用稳定的站点绝对路径引用。
- 不编辑或提交 `docs/.vitepress/dist/` 和 `docs/node_modules/`。

## 验证

从仓库根目录运行：

```bash
pnpm --dir docs build
```

构建必须无断链、无 VitePress 配置错误，并确认中英文页面都能生成。
