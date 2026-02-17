# Frontend Overview

Art Admin frontend is built on **[art-design-pro](https://www.artd.pro)** 3.0.1, an enterprise-grade admin template.

## Tech Stack

| Tech | Version | Purpose |
| --- | --- | --- |
| Vue 3 | 3.5+ | `<script setup>` Composition API |
| Vite | 7.x | Lightning-fast build tool |
| Element Plus | 2.x | UI component library |
| TailwindCSS | 4.x | Utility-first CSS |
| Pinia | 2.x | State management |
| Axios | 1.x | HTTP client |
| TypeScript | 5.x | Type system |

## Auto Imports

The following APIs are available without explicit `import`, handled by `unplugin-auto-import`:

```ts
// Vue core
ref, reactive, computed, watch, watchEffect
onMounted, onUnmounted, nextTick

// Vue Router
useRouter, useRoute

// VueUse
useStorage, useDebounceFn, useThrottleFn
```

## Path Aliases

| Alias | Actual Path |
| --- | --- |
| `@` | `src/` |
| `@views` | `src/views/` |

## Directory Structure

```
src/
├── api/            # API wrappers
├── assets/         # Static resources
├── components/     # Global components (ArtTable, ArtSearchBar...)
├── composables/    # Composable functions
├── directives/     # Custom directives (v-auth)
├── enums/          # Static enums
├── hooks/          # Core hooks (useTable, useAuth)
├── router/         # Route config
├── store/          # Pinia stores
├── utils/          # Utilities (http, dict...)
└── views/          # Page views
```

## Backend Routing Mode

::: warning Important
Art Admin uses **backend routing mode** — menus are driven by the `sys_menu` database table. **Do not modify frontend static route files** (`asyncRoutes.ts`, `routesAlias.ts`). New pages must have corresponding `sys_menu` records.
:::

## More Documentation

For general art-design-pro features (themes, layouts, i18n), refer to the official docs:

- [art-design-pro Docs](https://www.artd.pro)
- [GitHub Repository](https://github.com/Jeremystu/art-design-pro)
