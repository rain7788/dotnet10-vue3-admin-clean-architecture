using Flurl.Http;
using Serilog;

namespace Art.Infra.Framework;

/// <summary>
/// Flurl HTTP 全局配置
/// </summary>
public static class FlurlConfiguration
{
    /// <summary>
    /// 配置 Flurl HTTP 全局设置
    /// </summary>
    /// <param name="defaultTimeoutSeconds">默认超时时间（秒），默认30秒</param>
    public static void Configure(int defaultTimeoutSeconds = 30)
    {
        FlurlHttp.Clients.WithDefaults(settings =>
        {
            settings.WithTimeout(defaultTimeoutSeconds);

            settings.OnError(async call =>
            {
                await LogFlurlError(call);
            });
        });
    }

    private static async Task LogFlurlError(FlurlCall call)
    {
        try
        {
            var request = call.Request;
            var url = request?.Url?.ToString() ?? "unknown";
            var method = request?.Verb?.Method ?? "unknown";
            var statusCode = call.Response?.StatusCode;
            var responseBody = string.Empty;

            try
            {
                if (call.Response != null)
                {
                    responseBody = await call.Response.GetStringAsync();
                }
            }
            catch
            {
                // 忽略读取响应体失败
            }

            // 截断过长的响应内容
            if (responseBody.Length > 1000)
            {
                responseBody = responseBody[..1000] + "...[truncated]";
            }

            Log.Error(
                call.Exception,
                "Flurl HTTP 请求失败: {Method} {Url}, 状态码: {StatusCode}, 响应: {Response}",
                method,
                url,
                statusCode,
                responseBody);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "记录 Flurl HTTP 错误日志时发生异常");
        }
    }
}
