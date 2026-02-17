# HTTP Client

Art Admin wraps Axios with built-in Token injection, global error handling, and automatic Token refresh retry.

## Import

```ts
import request from '@/utils/http'
```

## Request Methods

```ts
// GET
request.get<T>({ url: '/admin/xxx/info', params: { id } })

// POST (params auto-converted to body)
request.post<T>({ url: '/admin/xxx/list', params: { pageIndex: 1, pageSize: 20 } })

// PUT
request.put<T>({ url: '/admin/xxx/update', data: formData })

// DELETE
request.del<T>({ url: `/admin/xxx/${id}` })
```

## Options

```ts
// Disable global error toast for this request
request.post<T>({ url: '/admin/xxx/save', data, showErrorMessage: false })

// Show success message
request.post<T>({ url: '/admin/xxx/save', data, showSuccessMessage: true })
```

## Built-in Behaviors

### Auto Token Injection

The request interceptor automatically adds the Bearer token from Pinia store:

```ts
axiosInstance.interceptors.request.use((request) => {
  const { accessToken } = useUserStore()
  if (accessToken)
    request.headers.set('Authorization', `Bearer ${accessToken}`)
  return request
})
```

### POST params → data

For `POST`/`PUT`, if only `params` is provided without `data`, params are automatically moved to the request body.

### Global Error Interception

::: danger No Duplicate Toasts
`src/utils/http` already has global `ElMessage.error` interception. In `catch` blocks, only restore state — **do NOT** show additional error messages!
:::

```ts
// ❌ Wrong — double toast
try { await fetchSaveUser(data) }
catch (e) { ElMessage.error('Save failed') }

// ✅ Correct — just restore state
try { await fetchSaveUser(data) }
catch (e) { loading.value = false }
```

### 401 Auto Logout

When the backend returns `code: 401`, user state is automatically cleared and redirected to the login page.

### Token Refresh Retry

Expired tokens are automatically refreshed with concurrent lock to prevent multiple simultaneous refresh requests.
