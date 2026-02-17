# HTTP 客户端

Art Admin 封装了基于 Axios 的 HTTP 客户端，内置 Token 注入、错误拦截、Token 刷新重试等功能。

## 导入

```ts
import request from '@/utils/http'
```

## 请求方法

```ts
// GET 请求
request.get<T>({ url: '/admin/xxx/info', params: { id } })

// POST 请求（params 自动转为 body）
request.post<T>({ url: '/admin/xxx/list', params: { pageIndex: 1, pageSize: 20 } })

// PUT 请求
request.put<T>({ url: '/admin/xxx/update', data: formData })

// DELETE 请求
request.del<T>({ url: `/admin/xxx/${id}` })
```

## 请求选项

```ts
// 关闭该请求的全局错误弹窗
request.post<T>({
  url: '/admin/xxx/save',
  data: formData,
  showErrorMessage: false
})

// 显示成功提示
request.post<T>({
  url: '/admin/xxx/save',
  data: formData,
  showSuccessMessage: true
})
```

## 内置行为

### Token 自动注入

请求拦截器自动从 Pinia Store 获取 Token 并添加到 Header：

```ts
axiosInstance.interceptors.request.use((request) => {
  const { accessToken } = useUserStore()
  if (accessToken)
    request.headers.set('Authorization', `Bearer ${accessToken}`)

  // 非 FormData 自动设置 Content-Type
  if (request.data && !(request.data instanceof FormData))
    request.headers.set('Content-Type', 'application/json')

  return request
})
```

### POST params 转 data

`POST`/`PUT` 请求中，如果只传了 `params` 没传 `data`，会自动将 `params` 移至 `data`（请求体）：

```ts
// 写法
request.post({ url: '/xxx', params: { name: 'test' } })

// 实际发送
POST /xxx
Body: { "name": "test" }
```

### 全局错误拦截

::: danger 禁止重复弹窗
`src/utils/http` 已有全局 `ElMessage.error` 拦截，`catch` 里只做状态还原，**不要**再弹错误提示！
:::

```ts
// ❌ 错误 — 会弹两次
try {
  await fetchSaveUser(formData)
} catch (e) {
  ElMessage.error('保存失败')  // 多余的！
}

// ✅ 正确 — 只还原状态
try {
  await fetchSaveUser(formData)
} catch (e) {
  loading.value = false
}
```

### 401 自动登出

当后端返回 `code: 401` 时，自动清除用户状态并跳转登录页。

### Token 刷新重试

Token 过期时自动刷新并重试原请求，使用并发锁避免多个请求同时刷新：

```ts
// 内部实现简要流程
if (response.status === 401 && !isRefreshing) {
  isRefreshing = true
  const newToken = await refreshToken()
  // 重试原请求
  return retryRequest(originalConfig)
}
```
