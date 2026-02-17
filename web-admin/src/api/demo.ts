import request from '@/utils/http'

// ===================== Demo 消息队列 =====================

// 入队消息
export function fetchEnqueueMessage(data: { message: string }) {
    return request.post<any>({
        url: '/admin/demo/queue/enqueue',
        data
    })
}

// 批量入队消息（最多100条）
export function fetchBatchEnqueueMessage(data: { messages: string[] }) {
    return request.post<any>({
        url: '/admin/demo/queue/enqueue/batch',
        data
    })
}

// 查询队列状态
export function fetchQueueStatus() {
    return request.get<{ queueLength: number }>({
        url: '/admin/demo/queue/status'
    })
}

// ===================== Demo 分布式锁 =====================

// TryLock：立即尝试获取锁
export function fetchTryLock(data: { holdSeconds?: number }) {
    return request.post<{
        acquired: boolean
        message: string
        lockKey: string
        heldForMs: number
    }>({
        url: '/admin/demo/lock/try',
        data
    })
}

// LockAsync：等待获取锁
export function fetchWaitLock(data: { holdSeconds?: number; waitSeconds?: number }) {
    return request.post<{
        acquired: boolean
        message: string
        lockKey: string
        heldForMs: number
    }>({
        url: '/admin/demo/lock/wait',
        data
    })
}

// 查询锁状态
export function fetchLockStatus() {
    return request.get<{
        isLocked: boolean
        lockKey: string
        remainingTtlSeconds: number
    }>({
        url: '/admin/demo/lock/status'
    })
}
