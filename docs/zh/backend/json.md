# JSON 序列化

Art Admin 统一配置 JSON 序列化行为，解决前端 `long` 精度丢失、日期格式不一致等常见问题。

## 核心配置

```csharp
// JsonConfiguration.cs
public static class JsonConfiguration
{
    public static JsonSerializerOptions GetDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        options.Converters.Add(new SmartLongConverter());
        options.Converters.Add(new FlexibleDateTimeConverter());
        options.Converters.Add(new FlexibleBoolConverter());

        return options;
    }
}
```

## SmartLongConverter

::: warning 前端精度问题
JavaScript 的 `Number.MAX_SAFE_INTEGER` 为 `2^53 - 1`（9007199254740991），而 .NET `long` 最大值为 `2^63 - 1`。雪花 ID 通常是 19 位数字，超出前端安全范围。
:::

`SmartLongConverter` 自动将超出安全范围的 `long` 值序列化为字符串：

```csharp
public class SmartLongConverter : JsonConverter<long>
{
    // 安全阈值：超过此值转为字符串
    private const long SafeMax = 9007199254740991;
    private const long SafeMin = -9007199254740991;

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        if (value > SafeMax || value < SafeMin)
            writer.WriteStringValue(value.ToString());
        else
            writer.WriteNumberValue(value);
    }

    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 同时支持数字和字符串格式的输入
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.String => long.Parse(reader.GetString()!),
            _ => throw new JsonException()
        };
    }
}
```

### 效果

```json
// 小值 → 数字
{ "status": 1 }

// 雪花 ID → 字符串
{ "id": "1234567890123456789" }
```

前端无需额外处理，直接作为字符串使用即可。

## FlexibleDateTimeConverter

灵活处理日期时间的序列化和反序列化，支持多种格式输入：

```csharp
public class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
    // 输出统一格式
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 支持 ISO 8601、自定义格式等多种输入
        var str = reader.GetString();
        return DateTime.Parse(str!);
    }
}
```

### 效果

```json
// 输出固定格式
{ "createdTime": "2025-03-15 10:30:00" }

// 输入可以是多种格式
"2025-03-15T10:30:00Z"     // ✅ ISO 8601
"2025-03-15 10:30:00"      // ✅ 自定义格式
"2025/03/15 10:30:00"      // ✅ 斜杠格式
```

## FlexibleBoolConverter

宽松的布尔值转换，兼容前端可能传来的各种布尔表示：

```csharp
public class FlexibleBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.GetInt32() != 0,    // 0/1
            JsonTokenType.String => reader.GetString()?.ToLower() is "true" or "1", // "true"/"1"
            _ => false
        };
    }
}
```

### 效果

```json
true      // ✅
false     // ✅
1         // ✅ → true
0         // ✅ → false
"true"    // ✅ → true
"1"       // ✅ → true
```

## 注册方式

JSON 全局配置在 `Program.cs` 中统一注册：

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    var defaults = JsonConfiguration.GetDefaultOptions();
    foreach (var converter in defaults.Converters)
        options.SerializerOptions.Converters.Add(converter);
    options.SerializerOptions.PropertyNamingPolicy = defaults.PropertyNamingPolicy;
});
```

所有 Minimal API 的请求反序列化和响应序列化自动使用该配置，无需在每个接口中指定。
