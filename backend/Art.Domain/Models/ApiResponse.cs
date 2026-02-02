namespace Art.Domain.Models;

/// <summary>
/// 分页响应模型
/// </summary>
public class PagedResponse
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
