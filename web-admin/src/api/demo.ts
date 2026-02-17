import request from '@/utils/http'

// ===================== Demo 消息队列 =====================

// 入队消息
export function fetchEnqueueMessage(data: { message: string }) {
    return request.post<any>({
        url: '/admin/demo/queue/enqueue',
        data
    })
}

// 查询队列状态
export function fetchQueueStatus() {
    return request.get<{ queueLength: number }>({
        url: '/admin/demo/queue/status'
    })
}
