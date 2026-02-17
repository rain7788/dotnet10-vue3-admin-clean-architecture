<template>
    <div class="pb-5">
        <h2 class="mb-1 text-lg font-medium">Redis 延迟消息队列示例</h2>
        <p class="mb-4 text-sm text-gray-500">
            演示基于 Redis Sorted Set 实现的延迟队列：消息在指定延迟后才会被消费
        </p>

        <ElCard shadow="never" class="art-card-xs">
            <!-- 队列状态 -->
            <div class="mb-4">
                <h3 class="mb-2 text-base font-medium">队列状态</h3>
                <div class="flex items-center gap-6 flex-wrap">
                    <div class="text-sm text-gray-600">
                        总消息数：<span class="text-lg font-semibold text-primary">{{ status.totalCount }}</span>
                    </div>
                    <div class="text-sm text-gray-600">
                        已到期 <span class="text-lg font-semibold text-red-500">{{ status.readyCount }}</span> 条
                    </div>
                    <div class="text-sm text-gray-600">
                        等待中 <span class="text-lg font-semibold text-orange-500">{{ status.pendingCount }}</span> 条
                    </div>
                    <div v-if="status.nextFireAtUtc" class="text-sm text-gray-500">
                        下一条到期：{{ formatTime(status.nextFireAtUtc) }}
                    </div>
                    <ElButton size="small" :loading="statusLoading" @click="refreshStatus">
                        刷新
                    </ElButton>
                </div>
            </div>

            <ElDivider />

            <!-- 投递消息 -->
            <div class="mb-4">
                <h3 class="mb-2 text-base font-medium">投递延迟消息</h3>
                <ElSpace wrap>
                    <ElInput v-model="messageInput" placeholder="输入消息内容" style="width: 260px"
                        @keyup.enter="handleEnqueue" />
                    <div class="flex items-center gap-2">
                        <ElInputNumber v-model="delaySeconds" :min="1" :max="300" :step="5" controls-position="right"
                            style="width: 150px" />
                        <span class="text-sm text-gray-500">秒后到期</span>
                    </div>
                    <ElButton type="primary" :loading="sending" @click="handleEnqueue">
                        投递
                    </ElButton>
                </ElSpace>
            </div>

            <!-- 快捷操作 -->
            <div class="mb-4">
                <h3 class="mb-2 text-base font-medium">快捷操作</h3>
                <ElSpace wrap>
                    <ElButton :loading="batchSending" :disabled="batchSending" @click="handleBatchSend(5, 10)">
                        5 条 · 10s 后到期
                    </ElButton>
                    <ElButton :loading="batchSending" :disabled="batchSending" @click="handleBatchSend(10, 30)">
                        10 条 · 30s 后到期
                    </ElButton>
                    <ElButton :loading="batchSending" :disabled="batchSending" @click="handleGradient">
                        梯度延迟（5/15/30/60s）
                    </ElButton>
                </ElSpace>
            </div>

            <ElDivider />

            <!-- 消息预览 -->
            <div>
                <div class="flex items-center justify-between mb-2">
                    <h3 class="text-base font-medium">消息预览（不消费）</h3>
                    <ElButton size="small" :loading="previewLoading" @click="refreshPreview">
                        刷新预览
                    </ElButton>
                </div>
                <ElTable v-if="previewList.length" :data="previewList" size="small" border stripe max-height="320">
                    <ElTableColumn label="消息内容" prop="message" min-width="200" show-overflow-tooltip />
                    <ElTableColumn label="状态" width="90" align="center">
                        <template #default="{ row }">
                            <ElTag :type="row.isReady ? 'success' : 'warning'" size="small">
                                {{ row.isReady ? '已到期' : '等待中' }}
                            </ElTag>
                        </template>
                    </ElTableColumn>
                    <ElTableColumn label="到期时间" width="180" align="center">
                        <template #default="{ row }">
                            {{ formatTime(row.fireAtUtc) }}
                        </template>
                    </ElTableColumn>
                    <ElTableColumn label="剩余" width="100" align="center">
                        <template #default="{ row }">
                            <span v-if="row.isReady" class="text-green-500">可消费</span>
                            <span v-else class="text-orange-500">{{ row.remainingSeconds.toFixed(0) }}s</span>
                        </template>
                    </ElTableColumn>
                </ElTable>
                <div v-else class="text-sm text-gray-400 py-4 text-center">
                    队列为空
                </div>
            </div>
        </ElCard>

        <!-- 消费说明 -->
        <ElCard shadow="never" class="art-card-xs mt-4">
            <div class="mb-4">
                <h3 class="mb-2 text-base font-medium">消费情况</h3>
                <ElAlert type="info" :closable="false">
                    <template #title>
                        后台任务每秒自动轮询到期消息，请查看后端控制台日志：
                    </template>
                    <div class="mt-2 text-sm">
                        <div>• 日志关键词：<code class="bg-gray-100 px-1 rounded">[DemoDelayQueue]</code></div>
                        <div>• 消息到期后输出：<code class="bg-gray-100 px-1 rounded">消费到期消息: xxx</code></div>
                    </div>
                </ElAlert>
            </div>

            <ElDivider />

            <!-- 用法说明 -->
            <h3 class="mb-2 text-base font-medium">后端用法说明</h3>
            <div class="text-sm text-gray-600 space-y-4">
                <div>
                    <p class="font-medium mb-1">投递延迟消息</p>
                    <pre class="bg-gray-50 rounded p-3 overflow-x-auto"><code>// 单条投递：10 秒后到期
                    _cache.DelayQueuePublish("order:timeout", orderId, TimeSpan.FromSeconds(10));

                    // 批量投递：不覆盖已有消息的延迟时间
                    _cache.DelayQueuePublishBatch("notify:batch", messages, TimeSpan.FromMinutes(5), overwrite:
                    false);</code></pre>
                </div>

                <div>
                    <p class="font-medium mb-1">消费到期消息（Worker 中调用）</p>
                    <pre class="bg-gray-50 rounded p-3 overflow-x-auto"><code>// 原子性消费：Lua 脚本保证 ZRANGEBYSCORE + ZREM 原子执行
                    var messages = _cache.DelayQueueConsume("order:timeout", maxCount: 20);
                    foreach (var msg in messages)
                    {
                    // 处理到期消息（如：关闭超时订单）
                    await CloseTimeoutOrder(msg);
                    }</code></pre>
                </div>

                <div>
                    <p class="font-medium mb-1">查询队列状态</p>
                    <pre class="bg-gray-50 rounded p-3 overflow-x-auto"><code>var info =
                    _cache.DelayQueueStatus("order:timeout");
                    // info.TotalCount — 总消息数
                    // info.ReadyCount — 已到期待消费
                    // info.PendingCount — 未到期等待中
                    // info.NextFireAtUtc — 下一条到期时间</code></pre>
                </div>

                <div>
                    <p class="font-medium mb-1">实现原理</p>
                    <ul class="list-disc list-inside pl-2">
                        <li><code class="bg-gray-100 px-1 rounded">ZADD</code> — score 为到期 Unix 时间戳（毫秒），member 为消息内容
                        </li>
                        <li><code class="bg-gray-100 px-1 rounded">ZRANGEBYSCORE + ZREM</code> — Lua
                            脚本原子取出到期消息并删除，多消费者安全</li>
                        <li><code class="bg-gray-100 px-1 rounded">overwrite</code> — true 用 ZADD（覆盖），false 用 ZADD
                            NX（不覆盖）</li>
                    </ul>
                </div>

                <div>
                    <p class="font-medium mb-1">适用场景</p>
                    <ul class="list-disc list-inside pl-2">
                        <li>订单超时自动关闭（下单后 30 分钟未支付）</li>
                        <li>延迟通知推送（活动开始前 10 分钟提醒）</li>
                        <li>定时重试（失败后 N 秒自动重试）</li>
                        <li>优惠券到期提醒</li>
                    </ul>
                </div>
            </div>
        </ElCard>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { ElMessage } from 'element-plus'
import {
    fetchDelayEnqueue,
    fetchDelayBatchEnqueue,
    fetchDelayQueueStatus,
    fetchDelayQueuePreview
} from '@/api/demo'

const messageInput = ref('')
const delaySeconds = ref(10)
const sending = ref(false)
const batchSending = ref(false)
const statusLoading = ref(false)
const previewLoading = ref(false)

const status = ref({ totalCount: 0, readyCount: 0, pendingCount: 0, nextFireAtUtc: null as string | null })
const previewList = ref<any[]>([])

let refreshTimer: number | null = null

const formatTime = (utcStr: string) => {
    if (!utcStr) return '-'
    const d = new Date(utcStr.endsWith('Z') ? utcStr : utcStr + 'Z')
    return d.toLocaleTimeString()
}

const refreshStatus = async () => {
    statusLoading.value = true
    try {
        status.value = await fetchDelayQueueStatus()
    } catch {
        // 全局拦截器已处理
    } finally {
        statusLoading.value = false
    }
}

const refreshPreview = async () => {
    previewLoading.value = true
    try {
        previewList.value = await fetchDelayQueuePreview(30)
    } catch {
        // 全局拦截器已处理
    } finally {
        previewLoading.value = false
    }
}

const refreshAll = async () => {
    await Promise.all([refreshStatus(), refreshPreview()])
}

const handleEnqueue = async () => {
    if (!messageInput.value.trim()) {
        ElMessage.warning('请输入消息内容')
        return
    }
    sending.value = true
    try {
        await fetchDelayEnqueue({
            message: messageInput.value,
            delaySeconds: delaySeconds.value
        })
        ElMessage.success(`已投递，${delaySeconds.value}s 后到期`)
        messageInput.value = ''
        await refreshAll()
    } catch {
        // 全局拦截器已处理
    } finally {
        sending.value = false
    }
}

const handleBatchSend = async (count: number, delay: number) => {
    batchSending.value = true
    try {
        const messages = Array.from({ length: count }, (_, i) =>
            `延迟测试 ${i + 1}/${count} - ${new Date().toLocaleTimeString()}`
        )
        await fetchDelayBatchEnqueue({ messages, delaySeconds: delay })
        ElMessage.success(`已批量投递 ${count} 条，${delay}s 后到期`)
        await refreshAll()
    } catch {
        // 全局拦截器已处理
    } finally {
        batchSending.value = false
    }
}

const handleGradient = async () => {
    batchSending.value = true
    try {
        const delays = [5, 15, 30, 60]
        for (const d of delays) {
            await fetchDelayEnqueue({
                message: `梯度消息 · ${d}s 后到期 - ${new Date().toLocaleTimeString()}`,
                delaySeconds: d
            })
        }
        ElMessage.success('已投递 4 条梯度延迟消息')
        await refreshAll()
    } catch {
        // 全局拦截器已处理
    } finally {
        batchSending.value = false
    }
}

onMounted(() => {
    refreshAll()
    refreshTimer = window.setInterval(() => refreshAll(), 2000)
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
