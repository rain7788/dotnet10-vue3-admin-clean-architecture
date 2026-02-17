<template>
    <div class="pb-5">
        <h2 class="mb-1 text-lg font-medium">Redis 消息队列示例</h2>
        <p class="mb-4 text-sm text-gray-500">
            演示如何使用 Redis List 实现持久化消息队列（LPUSH 入队 + RPOP 消费）
        </p>

        <ElCard shadow="never" class="art-card-xs">
            <div class="mb-4">
                <h3 class="mb-2 text-base font-medium">发送消息</h3>
                <ElSpace>
                    <ElInput v-model="messageInput" placeholder="输入消息内容" style="width: 300px"
                        @keyup.enter="handleEnqueue" />
                    <ElButton type="primary" :loading="sending" @click="handleEnqueue">
                        发送消息
                    </ElButton>
                </ElSpace>
            </div>

            <ElDivider />

            <div>
                <h3 class="mb-2 text-base font-medium">队列状态</h3>
                <div class="flex items-center gap-4">
                    <div class="text-sm text-gray-600">
                        当前队列长度：
                        <span class="text-lg font-semibold text-primary">{{ queueLength }}</span>
                        条消息待消费
                    </div>
                    <ElButton size="small" :loading="refreshing" @click="refreshQueueStatus">
                        刷新状态
                    </ElButton>
                </div>
            </div>

            <ElDivider />

            <div>
                <h3 class="mb-2 text-base font-medium">消费情况</h3>
                <ElAlert type="info" :closable="false">
                    <template #title>
                        后台任务每秒自动消费队列消息（RPOP），请查看后端控制台日志：
                    </template>
                    <div class="mt-2 text-sm">
                        <div>• 日志关键词：<code class="bg-gray-100 px-1 rounded">[DemoQueue]</code></div>
                        <div>
                            • 每条消息会在日志中输出：<code class="bg-gray-100 px-1 rounded">消费消息: xxx</code>
                        </div>
                        <div>
                            • 批量消费完成后输出：<code class="bg-gray-100 px-1 rounded">本轮处理完成: N</code>
                        </div>
                    </div>
                </ElAlert>
            </div>

            <ElDivider />

            <div>
                <h3 class="mb-2 text-base font-medium">快捷操作</h3>
                <ElSpace wrap>
                    <ElButton @click="handleBatchSend(5)">批量发送 5 条</ElButton>
                    <ElButton @click="handleBatchSend(20)">批量发送 20 条</ElButton>
                    <ElButton @click="handleBatchSend(100)">批量发送 100 条</ElButton>
                </ElSpace>
            </div>
        </ElCard>

        <ElCard shadow="never" class="art-card-xs mt-4">
            <h3 class="mb-2 text-base font-medium">实现说明</h3>
            <div class="text-sm text-gray-600 space-y-2">
                <p><strong>后端实现：</strong></p>
                <ul class="list-disc list-inside pl-2">
                    <li>
                        <code class="bg-gray-100 px-1 rounded">POST /admin/demo/queue/enqueue</code> - 使用
                        Redis LPUSH 从左侧入队
                    </li>
                    <li>
                        <code class="bg-gray-100 px-1 rounded">GET /admin/demo/queue/status</code> - 使用
                        Redis LLEN 查询队列长度
                    </li>
                    <li>
                        后台任务
                        <code class="bg-gray-100 px-1 rounded">DemoMessageQueueWorker</code> 使用 Redis
                        RPOP 从右侧消费（FIFO 先进先出）
                    </li>
                </ul>
                <p class="mt-3"><strong>任务调度配置：</strong></p>
                <ul class="list-disc list-inside pl-2">
                    <li>
                        <code class="bg-gray-100 px-1 rounded">AddLongRunningTask</code> -
                        长期运行任务，适合队列消费场景
                    </li>
                    <li><code class="bg-gray-100 px-1 rounded">interval: 1s</code> - 每秒尝试获取分布式锁</li>
                    <li>
                        <code class="bg-gray-100 px-1 rounded">processingInterval: 20ms</code> -
                        每次处理后延迟 20ms 避免 CPU 空转
                    </li>
                    <li>
                        <code class="bg-gray-100 px-1 rounded">runDuration: 30s</code> - 单次运行窗口最多
                        30 秒，之后释放锁让其他 Pod 轮换
                    </li>
                </ul>
                <p class="mt-3"><strong>适用场景：</strong></p>
                <ul class="list-disc list-inside pl-2">
                    <li>异步任务处理（发送邮件、短信、推送通知等）</li>
                    <li>削峰填谷（高峰期缓冲请求，平稳处理）</li>
                    <li>解耦业务模块（生产者和消费者分离）</li>
                </ul>
            </div>
        </ElCard>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { ElMessage } from 'element-plus'
import { fetchEnqueueMessage, fetchBatchEnqueueMessage, fetchQueueStatus } from '@/api/demo'

const messageInput = ref('')
const queueLength = ref(0)
const sending = ref(false)
const refreshing = ref(false)
let refreshTimer: number | null = null

// 发送消息
const handleEnqueue = async () => {
    if (!messageInput.value.trim()) {
        ElMessage.warning('请输入消息内容')
        return
    }

    sending.value = true
    try {
        await fetchEnqueueMessage({ message: messageInput.value })
        ElMessage.success('消息已入队')
        messageInput.value = ''
        await refreshQueueStatus()
    } catch {
        // 全局拦截器已处理错误弹窗
    } finally {
        sending.value = false
    }
}

// 刷新队列状态
const refreshQueueStatus = async () => {
    refreshing.value = true
    try {
        const res = await fetchQueueStatus()
        queueLength.value = res.queueLength || 0
    } catch (error) {
        console.error('获取队列状态失败', error)
    } finally {
        refreshing.value = false
    }
}

// 批量发送
const handleBatchSend = async (count: number) => {
    sending.value = true
    try {
        const messages = Array.from({ length: count }, (_, i) =>
            `批量测试消息 ${i + 1}/${count} - ${new Date().toLocaleTimeString()}`
        )
        await fetchBatchEnqueueMessage({ messages })
        ElMessage.success(`已批量发送 ${count} 条消息`)
        await refreshQueueStatus()
    } catch {
        // 全局拦截器已处理错误弹窗
    } finally {
        sending.value = false
    }
}

// 自动刷新队列状态（每 2 秒）
const startAutoRefresh = () => {
    refreshTimer = window.setInterval(() => {
        refreshQueueStatus()
    }, 2000)
}

const stopAutoRefresh = () => {
    if (refreshTimer) {
        clearInterval(refreshTimer)
        refreshTimer = null
    }
}

onMounted(() => {
    refreshQueueStatus()
    startAutoRefresh()
})

onUnmounted(() => {
    stopAutoRefresh()
})
</script>

<style scoped>
code {
    font-family: 'Courier New', monospace;
    font-size: 0.9em;
}
</style>
