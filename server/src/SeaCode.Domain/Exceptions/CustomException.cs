namespace SeaCode.Domain.Exceptions;

/// <summary>
/// 自定义异常基类
/// </summary>
public class CustomException : Exception
{
    /// <summary>
    /// 错误码（字符串类型，便于扩展）
    /// </summary>
    public int ErrorCode { get; set; }

    public CustomException() : base() { }

    public CustomException(string message) : base(message) { }

    public CustomException(int errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public CustomException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 400 Bad Request - 请求参数错误或业务规则校验失败
/// </summary>
public class BadRequestException : CustomException
{
    public BadRequestException(string message) : base(message) { }
    public BadRequestException(int errorCode, string message) : base(errorCode, message) { }
}

/// <summary>
/// 401 Unauthorized - 未认证
/// </summary>
public class UnauthorizedException : CustomException
{
    public UnauthorizedException(string message = "未登录或登录已过期") : base(message) { }
    public UnauthorizedException(int errorCode, string message) : base(errorCode, message) { }
}

/// <summary>
/// 403 Forbidden - 无权限访问
/// </summary>
public class ForbiddenException : CustomException
{
    public ForbiddenException(string message = "无权限访问") : base(message) { }
    public ForbiddenException(int errorCode, string message) : base(errorCode, message) { }
}

/// <summary>
/// 404 Not Found - 资源不存在
/// </summary>
public class NotFoundException : CustomException
{
    public NotFoundException(string message = "资源不存在") : base(message) { }
    public NotFoundException(int errorCode, string message) : base(errorCode, message) { }
}

/// <summary>
/// 406 Not Acceptable - 不可接受的请求（如限购、库存不足等）
/// </summary>
public class NotAcceptableException : CustomException
{
    public NotAcceptableException(string message) : base(message) { }
    public NotAcceptableException(int errorCode, string message) : base(errorCode, message) { }
}

/// <summary>
/// 500 Internal Server Error - 服务器内部错误
/// </summary>
public class InternalServerException : CustomException
{
    public InternalServerException(string message = "服务器内部错误") : base(message) { }
    public InternalServerException(int errorCode, string message) : base(errorCode, message) { }
}

/// <summary>
/// 第三方服务异常
/// </summary>
public class ThirdPartyServerException : CustomException
{
    public ThirdPartyServerException(string message) : base(message) { }
    public ThirdPartyServerException(int errorCode, string message) : base(errorCode, message) { }
}
