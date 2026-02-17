import { defineConfig } from 'vitepress'

export default defineConfig({
    title: 'Art Admin',
    description: 'AI-Friendly Full-Stack Admin Framework',

    // 自定义域名部署，base 设为 /
    base: '/',

    head: [
        ['link', { rel: 'icon', type: 'image/svg+xml', href: '/logo.svg' }],
    ],

    // 多语言
    locales: {
        zh: {
            label: '简体中文',
            lang: 'zh-CN',
            link: '/zh/',
            themeConfig: {
                nav: [
                    { text: '文档', link: '/zh/guide/introduction' },
                    { text: '在线演示', link: 'https://admin.aftbay.com' },
                    { text: 'Swagger', link: 'https://api.aftbay.com/swagger' },
                ],
                sidebar: {
                    '/zh/': [
                        {
                            text: '指南',
                            items: [
                                { text: '介绍', link: '/zh/guide/introduction' },
                                { text: '快速开始', link: '/zh/guide/quick-start' },
                                { text: '项目结构', link: '/zh/guide/project-structure' },
                                { text: '设计理念', link: '/zh/guide/design-philosophy' },
                            ]
                        },
                        {
                            text: '后端',
                            items: [
                                { text: '架构总览', link: '/zh/backend/architecture' },
                                { text: '自动依赖注入', link: '/zh/backend/auto-di' },
                                { text: '路由系统', link: '/zh/backend/routing' },
                                {
                                    text: 'EF Core 数据库',
                                    collapsed: false,
                                    items: [
                                        { text: '实体与导航属性', link: '/zh/backend/ef-core/entity' },
                                        { text: 'PredicateBuilder 动态查询', link: '/zh/backend/ef-core/dynamic-query' },
                                        { text: '乐观锁并发控制', link: '/zh/backend/ef-core/concurrency' },
                                    ]
                                },
                                { text: '多租户', link: '/zh/backend/multi-tenancy' },
                                { text: '认证鉴权', link: '/zh/backend/authentication' },
                                { text: '异常处理', link: '/zh/backend/exception' },
                                { text: '雪花 ID', link: '/zh/backend/snowflake-id' },
                                { text: 'Redis 缓存', link: '/zh/backend/cache' },
                                { text: '分布式锁', link: '/zh/backend/distributed-lock' },
                                { text: '消息队列', link: '/zh/backend/message-queue' },
                                { text: '延迟队列', link: '/zh/backend/delay-queue' },
                                { text: '定时任务与长任务', link: '/zh/backend/task-scheduler' },
                                { text: 'Serilog 日志', link: '/zh/backend/logging' },
                                { text: 'Swagger UI', link: '/zh/backend/swagger' },
                                { text: 'JSON 序列化', link: '/zh/backend/json' },
                            ]
                        },
                        {
                            text: '前端',
                            items: [
                                { text: '介绍', link: '/zh/frontend/introduction' },
                                { text: 'HTTP 请求封装', link: '/zh/frontend/http-client' },
                                { text: 'API 对接规范', link: '/zh/frontend/api-integration' },
                                { text: 'useTable 表格方案', link: '/zh/frontend/use-table' },
                                { text: '权限与路由', link: '/zh/frontend/permission' },
                                { text: '枚举字典', link: '/zh/frontend/enum-dict' },
                            ]
                        },
                        {
                            text: '数据库',
                            items: [
                                { text: '表结构设计', link: '/zh/database/schema' },
                                { text: '迁移管理', link: '/zh/database/migration' },
                            ]
                        },
                        {
                            text: '部署',
                            items: [
                                { text: 'Docker 部署', link: '/zh/deployment/docker' },
                                { text: 'Kubernetes 部署', link: '/zh/deployment/kubernetes' },
                                { text: 'CI/CD', link: '/zh/deployment/ci-cd' },
                            ]
                        },
                    ]
                },
                outline: {
                    label: '本页目录',
                },
                editLink: {
                    pattern: 'https://github.com/rain7788/art-admin/edit/main/docs/:path',
                    text: '在 GitHub 上编辑此页',
                },
                lastUpdated: {
                    text: '最后更新于',
                },
                docFooter: {
                    prev: '上一页',
                    next: '下一页',
                },
                footer: {
                    message: '根据 MIT 许可证发布',
                    copyright: 'Copyright © 2024-present Art Admin',
                },
            },
        },
        en: {
            label: 'English',
            lang: 'en-US',
            link: '/en/',
            themeConfig: {
                nav: [
                    { text: 'Docs', link: '/en/guide/introduction' },
                    { text: 'Live Demo', link: 'https://admin.aftbay.com' },
                    { text: 'Swagger', link: 'https://api.aftbay.com/swagger' },
                ],
                sidebar: {
                    '/en/': [
                        {
                            text: 'Guide',
                            items: [
                                { text: 'Introduction', link: '/en/guide/introduction' },
                                { text: 'Quick Start', link: '/en/guide/quick-start' },
                                { text: 'Project Structure', link: '/en/guide/project-structure' },
                                { text: 'Design Philosophy', link: '/en/guide/design-philosophy' },
                            ]
                        },
                        {
                            text: 'Backend',
                            items: [
                                { text: 'Architecture Overview', link: '/en/backend/architecture' },
                                { text: 'Auto Dependency Injection', link: '/en/backend/auto-di' },
                                { text: 'Routing System', link: '/en/backend/routing' },
                                {
                                    text: 'EF Core Database',
                                    collapsed: false,
                                    items: [
                                        { text: 'Entity & Navigation', link: '/en/backend/ef-core/entity' },
                                        { text: 'Dynamic Query', link: '/en/backend/ef-core/dynamic-query' },
                                        { text: 'Optimistic Concurrency', link: '/en/backend/ef-core/concurrency' },
                                    ]
                                },
                                { text: 'Multi-Tenancy', link: '/en/backend/multi-tenancy' },
                                { text: 'Authentication', link: '/en/backend/authentication' },
                                { text: 'Exception Handling', link: '/en/backend/exception' },
                                { text: 'Snowflake ID', link: '/en/backend/snowflake-id' },
                                { text: 'Redis Cache', link: '/en/backend/cache' },
                                { text: 'Distributed Lock', link: '/en/backend/distributed-lock' },
                                { text: 'Message Queue', link: '/en/backend/message-queue' },
                                { text: 'Delay Queue', link: '/en/backend/delay-queue' },
                                { text: 'Task Scheduler', link: '/en/backend/task-scheduler' },
                                { text: 'Serilog Logging', link: '/en/backend/logging' },
                                { text: 'Swagger UI', link: '/en/backend/swagger' },
                                { text: 'JSON Serialization', link: '/en/backend/json' },
                            ]
                        },
                        {
                            text: 'Frontend',
                            items: [
                                { text: 'Introduction', link: '/en/frontend/introduction' },
                                { text: 'HTTP Client', link: '/en/frontend/http-client' },
                                { text: 'API Integration', link: '/en/frontend/api-integration' },
                                { text: 'useTable', link: '/en/frontend/use-table' },
                                { text: 'Permission & Route', link: '/en/frontend/permission' },
                                { text: 'Enum Dictionary', link: '/en/frontend/enum-dict' },
                            ]
                        },
                        {
                            text: 'Database',
                            items: [
                                { text: 'Schema Design', link: '/en/database/schema' },
                                { text: 'Migration', link: '/en/database/migration' },
                            ]
                        },
                        {
                            text: 'Deployment',
                            items: [
                                { text: 'Docker', link: '/en/deployment/docker' },
                                { text: 'Kubernetes', link: '/en/deployment/kubernetes' },
                                { text: 'CI/CD', link: '/en/deployment/ci-cd' },
                            ]
                        },
                    ]
                },
                editLink: {
                    pattern: 'https://github.com/rain7788/art-admin/edit/main/docs/:path',
                    text: 'Edit this page on GitHub',
                },
                footer: {
                    message: 'Released under the MIT License',
                    copyright: 'Copyright © 2024-present Art Admin',
                },
            },
        },
    },

    themeConfig: {
        logo: '/logo.svg',
        socialLinks: [
            { icon: 'github', link: 'https://github.com/rain7788/art-admin' },
        ],
        search: {
            provider: 'local',
        },
    },
})
