using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SeaCode.Domain.Exceptions;
using SeaCode.Infra.Common;
using Serilog;

namespace SeaCode.Infra.Framework.Middlewares;

/// <summary>
/// 全局异常处理中间件
/// 根据异常类型映射 HTTP 状态码，统一返回 {code, msg} 格式
/// 开发环境会显示真实错误信息，方便调试
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IDiagnosticContext _diagnosticContext;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = JsonConfiguration.DefaultOptions;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment, IDiagnosticContext diagnosticContext)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _diagnosticContext = diagnosticContext;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // 客户端主动断开连接，不记录错误日志
            _logger.LogDebug("Request aborted by client: {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // 检查客户端是否已断开
        if (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("Client disconnected before error response: {Path}", context.Request.Path);
            return;
        }

        // 检查响应是否已开始发送
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response has already started, cannot write error response for: {Message}", ex.Message);
            return;
        }

        var statusCode = GetHttpStatusCode(ex);
        var errorCode = GetErrorCode(ex, statusCode);
        var message = ex.Message;

        // 非业务异常记录详细日志
        if (ex is not CustomException)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            // 开发环境显示真实错误信息，方便调试；生产环境隐藏内部异常详情
            if (!_environment.IsDevelopment())
            {
                message = "服务器错误";
            }
        }
        else if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(ex, "Internal server exception: {Message}", ex.Message);
        }
        // else
        // {
        //     // 业务异常只记录 Warning 级别
        //     _logger.LogWarning("Business exception: {Code} - {Message}", errorCode, message);
        // }

        // 记录错误响应到 DiagnosticContext（用于日志字段提取）
        LogErrorToDiagnosticContext(errorCode, message);

        await WriteErrorResponse(context, statusCode, errorCode, message);
    }

    /// <summary>
    /// 根据异常类型映射 HTTP 状态码
    /// </summary>
    private static HttpStatusCode GetHttpStatusCode(Exception ex)
    {
        return ex switch
        {
            BadHttpRequestException => HttpStatusCode.BadRequest,         // 400
            BadRequestException => HttpStatusCode.BadRequest,             // 400
            UnauthorizedException => HttpStatusCode.Unauthorized,         // 401
            ForbiddenException => HttpStatusCode.Forbidden,               // 403
            NotFoundException => HttpStatusCode.NotFound,                 // 404
            NotAcceptableException => HttpStatusCode.NotAcceptable,       // 406
            InternalServerException => HttpStatusCode.InternalServerError,// 500
            ThirdPartyServerException => HttpStatusCode.BadGateway,       // 502
            _ => HttpStatusCode.InternalServerError                       // 500
        };
    }

    /// <summary>
    /// 获取错误码（优先使用自定义错误码，否则使用 HTTP 状态码）
    /// </summary>
    private static int GetErrorCode(Exception ex, HttpStatusCode statusCode)
    {
        if (ex is CustomException customEx && customEx.ErrorCode != 0)
        {
            return customEx.ErrorCode;
        }
        return (int)statusCode;
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, int code, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse { Code = code, Msg = message };
        var json = System.Text.Json.JsonSerializer.Serialize(response, JsonOptions);

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// 记录错误响应到 DiagnosticContext
    /// </summary>
    private void LogErrorToDiagnosticContext(int code, string message)
    {
        var response = new ErrorResponse { Code = code, Msg = message };
        var json = System.Text.Json.JsonSerializer.Serialize(response, JsonOptions);
        _diagnosticContext.Set("Response", json);
    }
}

/// <summary>
/// 错误响应模型
/// </summary>
public class ErrorResponse
{
    public int Code { get; set; }
    public string? Msg { get; set; }
}
