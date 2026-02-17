---
layout: home

hero:
  name: Art Admin
  text: AI 友好的全栈后台框架
  tagline: 基于 .NET 10 Minimal API + Vue 3 + Element Plus，清洁架构、开箱即用、为 AI 协作开发而设计
  actions:
    - theme: brand
      text: 快速开始 →
      link: /zh/guide/quick-start
    - theme: alt
      text: 在线演示
      link: https://admin.aftbay.com
    - theme: alt
      text: GitHub
      link: https://github.com/rain7788/dotnet10-vue3-admin-clean-architecture
  image:
    src: /logo.svg
    alt: Art Admin

features:
  - icon: 🤖
    title: AI 友好架构
    details: 清洁分层、约定优于配置、代码结构一致。AI 可以直接读懂并生成符合框架规范的代码，开发效率倍增。
  - icon: 🏗️
    title: .NET 10 Minimal API
    details: 自研四层架构（Api → Core → Domain ← Infra），自动依赖注入、自动路由注册、零样板代码。
  - icon: 🎨
    title: 现代化前端 UI
    details: 基于 art-design-pro，Vue 3 + Element Plus + TailwindCSS 4，高颜值、开箱即用的中后台界面。
  - icon: 🔐
    title: 完善的权限体系
    details: Reference Token 认证、RBAC 权限控制、多端 API 隔离（Admin / App / Common）、按钮级别权限。
  - icon: 📦
    title: 开箱即用基础设施
    details: 分布式锁、消息队列、延迟队列、雪花 ID、定时任务、Serilog 按天分表日志，全部基于 Redis。
  - icon: 🏢
    title: 多租户支持
    details: 实体实现 ITenantEntity 即自动启用租户过滤，SaveChanges 自动填充 TenantId，零侵入。
---

## 技术栈

<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin: 24px 0;">

<div>

### 后端

- **.NET 10** — Minimal API 架构
- **EF Core 9** + MySQL 8.0
- **Redis** — 缓存 / 锁 / 队列
- **Serilog** — 结构化日志
- **LinqKit** — 动态查询
- **Yitter** — 雪花 ID

</div>
<div>

### 前端

- **Vue 3** — Composition API
- **Element Plus** — UI 组件库
- **TailwindCSS 4** — 原子化 CSS
- **Vite 7** — 极速构建
- **Pinia** — 状态管理
- **Axios** — HTTP 客户端

</div>
</div>
