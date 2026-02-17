using System.Text;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Art.Infra.Framework.Middlewares;

/// <summary>
/// 请求/响应内容记录中间件
/// 将请求体和响应体记录到 Serilog DiagnosticContext，供日志字段提取使用
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDiagnosticContext _diagnosticContext;

    /// <summary>
    /// 请求/响应体最大记录长度（10KB）
    /// </summary>
    private const int MaxContentLength = 10 * 1024;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        IDiagnosticContext diagnosticContext)
    {
        _next = next;
        _diagnosticContext = diagnosticContext;
    }

    public async Task Invoke(HttpContext context)
    {
        // 启用请求体缓冲（允许多次读取）
        context.Request.EnableBuffering();

        var originalBodyStream = context.Response.Body;
        await using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            // 记录请求参数（异步方式读取）
            var requestParams = await GetRequestParamsAsync(context);
            if (!string.IsNullOrEmpty(requestParams))
                _diagnosticContext.Set("Request", TruncateContent(requestParams));

            await _next(context);

            // 记录响应内容
            if (responseBodyStream.CanSeek && responseBodyStream.Length > 0)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                responseBodyStream.Seek(0, SeekOrigin.Begin);

                if (!string.IsNullOrEmpty(responseBody))
                    _diagnosticContext.Set("Response", TruncateContent(responseBody));
            }
        }
        finally
        {
            await RestoreResponseBodyAsync(context, originalBodyStream, responseBodyStream);
        }
    }

    /// <summary>
    /// 获取请求参数（异步方式）
    /// GET/DELETE: 返回 QueryString
    /// POST/PUT: 返回请求体
    /// </summary>
    private static async Task<string> GetRequestParamsAsync(HttpContext context)
    {
        try
        {
            var method = context.Request.Method.ToUpper();

            if (method == "GET" || method == "DELETE")
                return context.Request.QueryString.Value ?? string.Empty;

            if (method == "POST" || method == "PUT" || method == "PATCH")
            {
                // 检查内容大小（如果指定了的话）
                if (context.Request.ContentLength.HasValue && context.Request.ContentLength > MaxContentLength)
                    return "[Content too large]";

                // 确保可以 seek
                if (context.Request.Body.CanSeek)
                    context.Request.Body.Seek(0, SeekOrigin.Begin);

                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();

                if (context.Request.Body.CanSeek)
                    context.Request.Body.Seek(0, SeekOrigin.Begin);

                return body ?? string.Empty;
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 恢复原始响应流
    /// </summary>
    private static async Task RestoreResponseBodyAsync(HttpContext context, Stream originalBody, MemoryStream responseBodyStream)
    {
        try
        {
            context.Response.Body = originalBody;

            if (responseBodyStream.CanSeek && responseBodyStream.Length > 0)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBody);
            }
        }
        catch
        {
            // 日志中间件不应影响主流程
        }
    }

    /// <summary>
    /// 截断内容
    /// </summary>
    private static string TruncateContent(string content)
    {
        if (string.IsNullOrEmpty(content) || content.Length <= MaxContentLength)
            return content;

        return content.Substring(0, MaxContentLength) + "... [truncated]";
    }
}
