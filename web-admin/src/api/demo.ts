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
