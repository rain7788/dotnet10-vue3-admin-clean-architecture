---
layout: home

hero:
  name: Art Admin
  text: AI-Friendly Full-Stack Admin Framework
  tagline: Built on .NET 10 Minimal API + Vue 3 + Element Plus. Clean architecture, batteries included, designed for AI-assisted development.
  actions:
    - theme: brand
      text: Get Started →
      link: /en/guide/quick-start
    - theme: alt
      text: Live Demo
      link: https://admin.aftbay.com
    - theme: alt
      text: GitHub
      link: https://github.com/rain7788/dotnet10-vue3-admin-clean-architecture
  image:
    src: /logo.svg
    alt: Art Admin

features:
  - icon: 🤖
    title: AI-Friendly Architecture
    details: Clean layering, convention over configuration, consistent code patterns. AI can read and generate framework-compliant code effortlessly.
  - icon: 🏗️
    title: .NET 10 Minimal API
    details: Custom four-layer architecture (Api → Core → Domain ← Infra), auto DI, auto route registration, zero boilerplate.
  - icon: 🎨
    title: Modern Frontend UI
    details: Built on art-design-pro with Vue 3 + Element Plus + TailwindCSS 4. Beautiful, production-ready admin interface.
  - icon: 🔐
    title: Complete Auth System
    details: Reference Token authentication, RBAC access control, multi-client API isolation (Admin / App / Common), button-level permissions.
  - icon: 📦
    title: Batteries Included
    details: Distributed lock, message queue, delay queue, Snowflake ID, scheduled tasks, Serilog daily-partitioned logging — all Redis-based.
  - icon: 🏢
    title: Multi-Tenancy
    details: Implement ITenantEntity to auto-enable tenant filtering. SaveChanges auto-fills TenantId. Zero intrusion.
---

## Tech Stack

<div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin: 24px 0;">

<div>

### Backend

- **.NET 10** — Minimal API
- **EF Core 9** + MySQL 8.0
- **Redis** — Cache / Lock / Queue
- **Serilog** — Structured Logging
- **LinqKit** — Dynamic Query
- **Yitter** — Snowflake ID

</div>
<div>

### Frontend

- **Vue 3** — Composition API
- **Element Plus** — UI Components
- **TailwindCSS 4** — Utility-first CSS
- **Vite 7** — Lightning Build
- **Pinia** — State Management
- **Axios** — HTTP Client

</div>
</div>
