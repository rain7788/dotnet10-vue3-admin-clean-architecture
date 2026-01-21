using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeaCode.Infra.Common;

/// <summary>
/// JSON 配置工具
/// 提供宽容的 JSON 序列化配置，兼容前端各种不规范参数
/// </summary>
public static class JsonConfiguration
{
    /// <summary>
    /// JavaScript Number.MAX_SAFE_INTEGER = 2^53 - 1 = 9007199254740991
    /// 超过此值的 long 会丢失精度，需要转为字符串
    /// </summary>
    public const long JsSafeMaxInteger = 9007199254740991L;

    /// <summary>
    /// JavaScript Number.MIN_SAFE_INTEGER = -(2^53 - 1) = -9007199254740991
    /// </summary>
    public const long JsSafeMinInteger = -9007199254740991L;

    /// <summary>
    /// 全局 JSON 配置（宽容模式）
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = CreateDefaultOptions();

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            // 驼峰命名
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

            // 忽略大小写（兼容前端不规范参数）
            PropertyNameCaseInsensitive = true,

            // 允许末尾逗号
            AllowTrailingCommas = true,

            // 跳过注释
            ReadCommentHandling = JsonCommentHandling.Skip,

            // 允许数字被引号包裹
            NumberHandling = JsonNumberHandling.AllowReadingFromString,

            // 忽略 null 值
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

            // 允许命名浮点数 (NaN, Infinity)
            // NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,

            // 编码配置（支持中文）
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // 添加自定义转换器
        options.Converters.Add(new FlexibleDateTimeConverter());
        options.Converters.Add(new FlexibleBoolConverter());
        options.Converters.Add(new SmartLongConverter());
        options.Converters.Add(new SmartNullableLongConverter());
        // 枚举序列化为数字，同时在模型中添加 XxxText 属性提供文本
        // options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));

        return options;
    }

    /// <summary>
    /// 配置 ASP.NET Core JSON 选项
    /// </summary>
    public static void ConfigureJsonOptions(Microsoft.AspNetCore.Http.Json.JsonOptions options)
    {
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.SerializerOptions.PropertyNameCaseInsensitive = true;
        options.SerializerOptions.AllowTrailingCommas = true;
        options.SerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
        options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.SerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        options.SerializerOptions.Converters.Add(new FlexibleDateTimeConverter());
        options.SerializerOptions.Converters.Add(new FlexibleBoolConverter());
        options.SerializerOptions.Converters.Add(new SmartLongConverter());
        options.SerializerOptions.Converters.Add(new SmartNullableLongConverter());
        // 枚举序列化为数字，同时在模型中添加 XxxText 属性提供文本
        // options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
    }
}

/// <summary>
/// 灵活的 DateTime 转换器
/// 支持多种日期格式
/// </summary>
public class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly string[] Formats = new[]
    {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd",
        "yyyy/MM/dd HH:mm:ss",
        "yyyy/MM/dd",
        "yyyyMMdd",
        "yyyyMMddHHmmss"
    };

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // 支持时间戳（毫秒）
            var timestamp = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime;
        }

        var str = reader.GetString();
        if (string.IsNullOrEmpty(str)) return default;

        // 尝试多种格式
        foreach (var format in Formats)
        {
            if (DateTime.TryParseExact(str, format, null, System.Globalization.DateTimeStyles.None, out var result))
            {
                return result;
            }
        }

        // 最后尝试默认解析
        if (DateTime.TryParse(str, out var dt))
        {
            return dt;
        }

        throw new JsonException($"无法解析日期格式: {str}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}

/// <summary>
/// 灵活的 Bool 转换器
/// 支持 "true"/"false"、"1"/"0"、1/0
/// </summary>
public class FlexibleBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.GetInt32() != 0,
            JsonTokenType.String => reader.GetString()?.ToLower() switch
            {
                "true" or "1" or "yes" => true,
                "false" or "0" or "no" or "" or null => false,
                var s => throw new JsonException($"无法将 '{s}' 转换为 bool")
            },
            _ => throw new JsonException($"无法将 {reader.TokenType} 转换为 bool")
        };
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}

/// <summary>
/// 智能 long 转换器
/// 读取时：支持字符串和数字
/// 写入时：超过 JS 安全整数范围则转为字符串，否则保持数字
/// </summary>
public class SmartLongConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.String => long.TryParse(reader.GetString(), out var val)
                ? val
                : throw new JsonException($"无法将 '{reader.GetString()}' 转换为 long"),
            _ => throw new JsonException($"无法将 {reader.TokenType} 转换为 long")
        };
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        // 所有 long 统一转为字符串，避免前端类型混乱
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// long? 转换器
/// 所有 long 统一输出为字符串
/// </summary>
public class SmartNullableLongConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.String => string.IsNullOrEmpty(reader.GetString())
                ? null
                : long.TryParse(reader.GetString(), out var val)
                    ? val
                    : throw new JsonException($"无法将 '{reader.GetString()}' 转换为 long?"),
            _ => throw new JsonException($"无法将 {reader.TokenType} 转换为 long?")
        };
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        // 所有 long 统一转为字符串
        writer.WriteStringValue(value.Value.ToString());
    }
}
