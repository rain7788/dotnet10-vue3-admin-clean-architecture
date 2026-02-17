using System.ComponentModel.DataAnnotations;
using Art.Domain.Exceptions;

namespace Art.Infra.Common;

/// <summary>
/// 参数验证工具
/// </summary>
public static class ParamsValidation
{
    /// <summary>
    /// 验证对象（使用 DataAnnotations）
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">要验证的对象</param>
    /// <exception cref="BadRequestException">验证失败时抛出</exception>
    public static void Validate<T>(T obj) where T : class
    {
        if (obj == null)
            throw new BadRequestException("请求参数不能为空");

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(obj);
        bool isValid = Validator.TryValidateObject(obj, validationContext, validationResults, validateAllProperties: true);

        if (!isValid)
        {
            var errorMessage = validationResults.FirstOrDefault()?.ErrorMessage ?? "参数验证失败";
            throw new BadRequestException(errorMessage);
        }
    }

    /// <summary>
    /// 验证并返回结果（不抛异常）
    /// </summary>
    public static (bool IsValid, List<string> Errors) TryValidate<T>(T obj) where T : class
    {
        if (obj == null)
            return (false, new List<string> { "请求参数不能为空" });

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(obj);
        bool isValid = Validator.TryValidateObject(obj, validationContext, validationResults, validateAllProperties: true);

        var errors = validationResults
            .Where(r => !string.IsNullOrEmpty(r.ErrorMessage))
            .Select(r => r.ErrorMessage!)
            .ToList();

        return (isValid, errors);
    }

    /// <summary>
    /// 验证字符串非空
    /// </summary>
    public static void NotNullOrEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BadRequestException($"{paramName}不能为空");
    }

    /// <summary>
    /// 验证数值范围
    /// </summary>
    public static void InRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
            throw new BadRequestException($"{paramName}必须在 {min} 到 {max} 之间");
    }

    /// <summary>
    /// 验证数值大于零
    /// </summary>
    public static void GreaterThanZero(int value, string paramName)
    {
        if (value <= 0)
            throw new BadRequestException($"{paramName}必须大于0");
    }

    /// <summary>
    /// 验证数值大于零
    /// </summary>
    public static void GreaterThanZero(decimal value, string paramName)
    {
        if (value <= 0)
            throw new BadRequestException($"{paramName}必须大于0");
    }

    /// <summary>
    /// 验证集合非空
    /// </summary>
    public static void NotEmpty<T>(IEnumerable<T>? collection, string paramName)
    {
        if (collection == null || !collection.Any())
            throw new BadRequestException($"{paramName}不能为空");
    }

    /// <summary>
    /// 验证字符串长度
    /// </summary>
    public static void Length(string? value, int minLength, int maxLength, string paramName)
    {
        if (value == null || value.Length < minLength || value.Length > maxLength)
            throw new BadRequestException($"{paramName}长度必须在 {minLength} 到 {maxLength} 之间");
    }

    /// <summary>
    /// 验证枚举值有效
    /// </summary>
    public static void ValidEnum<TEnum>(TEnum value, string paramName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
            throw new BadRequestException($"{paramName}的值无效");
    }
}

/// <summary>
/// 验证扩展方法
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// 验证对象（链式调用）
    /// </summary>
    public static T Validated<T>(this T obj) where T : class
    {
        ParamsValidation.Validate(obj);
        return obj;
    }

    /// <summary>
    /// 确保非空
    /// </summary>
    public static T EnsureNotNull<T>(this T? obj, string message = "对象不能为空") where T : class
    {
        if (obj == null)
            throw new BadRequestException(message);
        return obj;
    }

    /// <summary>
    /// 确保非空（NotFoundException）
    /// </summary>
    public static T EnsureFound<T>(this T? obj, string message = "资源不存在") where T : class
    {
        if (obj == null)
            throw new NotFoundException(message);
        return obj;
    }
}
