/**
 * 文章相关 API
 * 目前使用本地 mock 数据，后续可对接真实后端接口
 */

import request from '@/utils/http'

// 文章分类数据（本地 mock）
const ARTICLE_TYPES = [
    { id: 1, name: '技术分享' },
    { id: 2, name: '生活随笔' },
    { id: 3, name: '学习笔记' },
    { id: 4, name: '项目经验' },
    { id: 5, name: '工具推荐' },
    { id: 6, name: '其他' }
]

// 获取文章分类列表
export function fetchGetArticleTypes() {
    return new Promise<{
        code: number
        data: typeof ARTICLE_TYPES
        message: string
    }>((resolve) => {
        // 模拟网络延迟 300ms
        setTimeout(() => {
            resolve({
                code: 200,
                data: ARTICLE_TYPES,
                message: 'success'
            })
        }, 300)
    })
}

// 发布文章
export function fetchPublishArticle(data: {
    title: string
    content: string
    categoryId: number
    cover?: string
    visible: boolean
}) {
    return request.post<any>({
        url: '/admin/article/publish',
        data
    })
}

// 获取文章详情（编辑模式使用）
export function fetchGetArticleDetail(id: string) {
    return new Promise<{
        code: number
        data: {
            title: string
            blog_class: string
            html_content: string
        }
        message: string
    }>((resolve) => {
        // 模拟网络延迟 300ms
        setTimeout(() => {
            resolve({
                code: 200,
                data: {
                    title: '示例文章标题',
                    blog_class: '1',
                    html_content: '<p>这是示例文章内容</p>'
                },
                message: 'success'
            })
        }, 300)
    })
}

// 获取文章列表
export function fetchGetArticleList(params: any) {
    return request.post<any>({
        url: '/admin/article/list',
        params
    })
}