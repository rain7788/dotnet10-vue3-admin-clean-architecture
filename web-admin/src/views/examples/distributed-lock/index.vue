<template>
    <div class="pb-5">
        <h2 class="mb-1 text-lg font-medium">Redis 分布式锁示例</h2>
        <p class="mb-4 text-sm text-gray-500">
            演示 Redis 分布式锁的两种获取方式：TryLock（立即返回）和 LockAsync（等待获取）
        </p>

        <ElCard shadow="never" class="art-card-xs">
            <!-- 锁状态 -->
            <div class="mb-4">
                <h3 class="mb-2 text-base font-medium">锁状态</h3>
                <div class="flex items-center gap-4">
                    <ElTag :type="lockStatus.isLocked ? 'danger' : 'success'" size="large">
                        {{ lockStatus.isLocked ? '🔒 已锁定' : '🔓 空闲' }}
                    </ElTag>
                    <span v-if="lockStatus.isLocked" class="text-sm text-gray-500">
                        剩余 TTL：{{ lockStatus.remainingTtlSeconds }}s
                    </span>
                    <ElButton size="small" :loading="statusLoading" @click="refreshStatus">
                        刷新状态
                    </ElButton>
                </div>
            </div>

            <ElDivider />

            <!-- TryLock 演示 -->
            <div class="mb-4">
                <h3 class="mb-2 text-base font-medium">
                    方式一：TryLock（立即返回）
                </h3>
                <p class="mb-3 text-sm text-gray-500">
                    尝试获取锁，无论成功或失败都<strong>立即返回</strong>，不会阻塞等待。适合"拿不到就放弃"的场景。
                </p>
                <ElSpace>
                    <ElInputNumber v-model="tryLockHoldSeconds" :min="1" :max="30" :step="1" controls-position="right"
                        style="width: 180px" />
                    <span class="text-sm text-gray-500">持有秒数</span>
                    <ElButton type="primary" :loading="tryLocking" @click="handleTryLock">
                        TryLock 获取锁
                    </ElButton>
                </ElSpace>
                <div v-if="tryLockResult" class="mt-3">
                    <ElAlert :type="tryLockResult.acquired ? 'success' : 'warning'" :closable="false">
                        <template #title>{{ tryLockResult.message }}</template>
                        <div v-if="tryLockResult.acquired" class="text-sm mt-1">
                            持有时长：{{ tryLockResult.heldForMs }}ms
                        </div>
                    </ElAlert>
                </div>
            </div>

            <ElDivider />

            <!-- LockAsync 演示 -->
            <div class="mb-4">
                <h3 class="mb-2 text-base font-medium">
                    方式二：LockAsync（等待获取）
                </h3>
                <p class="mb-3 text-sm text-gray-500">
                    等待获取锁，如果锁被占用会<strong>轮询重试</strong>直到获取成功或超时。适合"必须拿到锁才能继续"的场景。
                </p>
                <ElSpace wrap>
                    <div class="flex items-center gap-2">
                        <ElInputNumber v-model="waitLockHoldSeconds" :min="1" :max="15" :step="1"
                            controls-position="right" style="width: 180px" />
                        <span class="text-sm text-gray-500">持有秒数</span>
                    </div>
                    <div class="flex items-center gap-2">
                        <ElInputNumber v-model="waitLockWaitSeconds" :min="1" :max="30" :step="1"
                            controls-position="right" style="width: 180px" />
                        <span class="text-sm text-gray-500">等待超时秒数</span>
                    </div>
                    <ElButton type="warning" :loading="waitLocking" @click="handleWaitLock">
                        LockAsync 等待获取锁
                    </ElButton>
                </ElSpace>
                <div v-if="waitLockResult" class="mt-3">
                    <ElAlert :type="waitLockResult.acquired ? 'success' : 'warning'" :closable="false">
                        <template #title>{{ waitLockResult.message }}</template>
                        <div v-if="waitLockResult.acquired" class="text-sm mt-1">
                            持有时长：{{ waitLockResult.heldForMs }}ms
                        </div>
                    </ElAlert>
                </div>
            </div>

            <ElDivider />

            <!-- 并发竞争演示 -->
            <div>
                <h3 class="mb-2 text-base font-medium">并发竞争演示</h3>
                <p class="mb-3 text-sm text-gray-500">
                    同时发起多个锁请求，观察锁的互斥效果。可以打开多个浏览器标签页同时操作来模拟多客户端竞争。
                </p>
                <ElSpace wrap>
                    <ElButton @click="handleConcurrentTryLock">
                        同时发起 3 个 TryLock
                    </ElButton>
                    <ElButton @click="handleConcurrentWaitLock">
                        同时发起 3 个 WaitLock
                    </ElButton>
                </ElSpace>
                <div v-if="concurrentResults.length" class="mt-3 space-y-2">
                    <ElAlert v-for="(r, i) in concurrentResults" :key="i" :type="r.acquired ? 'success' : 'warning'"
                        :closable="false">
                        <template #title>请求 {{ i + 1 }}：{{ r.message }}</template>
                    </ElAlert>
                </div>
            </div>
        </ElCard>

        <!-- 用法说明 -->
        <ElCard shadow="never" class="art-card-xs mt-4">
            <h3 class="mb-2 text-base font-medium">后端用法说明</h3>
            <div class="text-sm text-gray-600 space-y-4">
                <div>
                    <p class="font-medium mb-1">方式一：TryLock — 立即返回（拿不到就放弃）</p>
                    <pre class="bg-gray-50 rounded p-3 overflow-x-auto"><code>// using 自动释放锁，locker 为 null 表示获取失败
                    using var locker = _cache.TryLock("my-resource", timeoutSeconds: 30);
                    if (locker == null)
                    {
                    throw new BadRequestException("操作正在进行中，请稍后再试");
                    }

                    // 获取成功，执行业务逻辑...
                    await DoSomethingAsync();</code></pre>
                </div>

                <div>
                    <p class="font-medium mb-1">方式二：LockAsync — 等待获取（排队等锁）</p>
                    <pre class="bg-gray-50 rounded p-3 overflow-x-auto"><code>// await using 异步释放，等待最多 10 秒
                    await using var locker = await _cache.LockAsync(
                    "my-resource",
                    timeout: TimeSpan.FromSeconds(30), // 锁超时时间
                    waitTimeout: TimeSpan.FromSeconds(10), // 等待获取超时
                    retryInterval: 200, // 重试间隔 ms
                    enableWatchdog: true); // 看门狗自动续期

                    if (locker == null)
                    {
                    throw new BadRequestException("获取锁超时，请稍后再试");
                    }

                    // 获取成功，执行业务逻辑...
                    await DoSomethingAsync();</code></pre>
                </div>

                <div>
                    <p class="font-medium mb-1">核心特性</p>
                    <ul class="list-disc list-inside pl-2">
                        <li><code class="bg-gray-100 px-1 rounded">SetNx</code> — 原子性获取锁，保证互斥</li>
                        <li><code class="bg-gray-100 px-1 rounded">Lua 脚本释放</code> —
                            只有持有者才能释放锁，防止误删</li>
                        <li><code class="bg-gray-100 px-1 rounded">看门狗续期</code> —
                            每 timeout/3 自动续期，防止业务未完成锁就过期</li>
                        <li><code class="bg-gray-100 px-1 rounded">IDisposable</code> / <code
                                class="bg-gray-100 px-1 rounded">IAsyncDisposable</code> — 支持 using / await using
                            自动释放</li>
                    </ul>
                </div>

                <div>
                    <p class="font-medium mb-1">适用场景</p>
                    <ul class="list-disc list-inside pl-2">
                        <li>防止重复提交（同一操作并发时只执行一次）</li>
                        <li>分布式任务调度（多个 Pod 只有一个执行）</li>
                        <li>库存扣减等需要原子操作的场景</li>
                        <li>限流降级（获取不到锁直接返回提示）</li>
                    </ul>
                </div>
            </div>
        </ElCard>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { fetchTryLock, fetchWaitLock, fetchLockStatus } from '@/api/demo'

const tryLockHoldSeconds = ref(5)
const waitLockHoldSeconds = ref(3)
const waitLockWaitSeconds = ref(10)

const tryLocking = ref(false)
const waitLocking = ref(false)
const statusLoading = ref(false)

const tryLockResult = ref<any>(null)
const waitLockResult = ref<any>(null)
const concurrentResults = ref<any[]>([])

const lockStatus = ref({ isLocked: false, lockKey: '', remainingTtlSeconds: 0 })

let refreshTimer: number | null = null

const refreshStatus = async () => {
    statusLoading.value = true
    try {
        const res = await fetchLockStatus()
        lockStatus.value = res
    } catch {
        // 全局拦截器已处理错误弹窗
    } finally {
        statusLoading.value = false
    }
}

const handleTryLock = async () => {
    tryLocking.value = true
    tryLockResult.value = null
    try {
        tryLockResult.value = await fetchTryLock({ holdSeconds: tryLockHoldSeconds.value })
        await refreshStatus()
    } catch {
        // 全局拦截器已处理错误弹窗
    } finally {
        tryLocking.value = false
    }
}

const handleWaitLock = async () => {
    waitLocking.value = true
    waitLockResult.value = null
    try {
        waitLockResult.value = await fetchWaitLock({
            holdSeconds: waitLockHoldSeconds.value,
            waitSeconds: waitLockWaitSeconds.value
        })
        await refreshStatus()
    } catch {
        // 全局拦截器已处理错误弹窗
    } finally {
        waitLocking.value = false
    }
}

const handleConcurrentTryLock = async () => {
    concurrentResults.value = []
    const promises = Array.from({ length: 3 }, () =>
        fetchTryLock({ holdSeconds: 5 }).catch(() => ({
            acquired: false,
            message: '请求失败',
            lockKey: '',
            heldForMs: 0
        }))
    )
    concurrentResults.value = await Promise.all(promises)
    await refreshStatus()
}

const handleConcurrentWaitLock = async () => {
    concurrentResults.value = []
    const promises = Array.from({ length: 3 }, () =>
        fetchWaitLock({ holdSeconds: 2, waitSeconds: 15 }).catch(() => ({
            acquired: false,
            message: '请求失败',
            lockKey: '',
            heldForMs: 0
        }))
    )
    concurrentResults.value = await Promise.all(promises)
    await refreshStatus()
}

onMounted(() => {
    refreshStatus()
    refreshTimer = window.setInterval(() => refreshStatus(), 2000)
})

onUnmounted(() => {
    if (refreshTimer) clearInterval(refreshTimer)
})
</script>

<style scoped>
code {
    font-family: 'Courier New', monospace;
    font-size: 0.9em;
}

pre {
    font-family: 'Courier New', monospace;
    font-size: 0.85em;
    line-height: 1.5;
}
</style>
