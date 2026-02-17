# JSON Serialization

Art Admin configures unified JSON serialization to solve common issues like frontend `long` precision loss and inconsistent date formats.

## Core Configuration

```csharp
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

::: warning Frontend Precision Issue
JavaScript's `Number.MAX_SAFE_INTEGER` is `2^53 - 1` (9007199254740991), while .NET `long` max is `2^63 - 1`. Snowflake IDs are typically 19 digits, exceeding the safe range.
:::

`SmartLongConverter` auto-serializes out-of-range `long` values as strings:

```csharp
public class SmartLongConverter : JsonConverter<long>
{
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
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.String => long.Parse(reader.GetString()!),
            _ => throw new JsonException()
        };
    }
}
```

### Result

```json
// Small value → number
{ "status": 1 }

// Snowflake ID → string
{ "id": "1234567890123456789" }
```

## FlexibleDateTimeConverter

Handles flexible date/time input formats with unified output:

```csharp
public class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Format));
}
```

```json
// Output: fixed format
{ "createdTime": "2025-03-15 10:30:00" }

// Input: accepts multiple formats
"2025-03-15T10:30:00Z"     // ✅ ISO 8601
"2025-03-15 10:30:00"      // ✅ Custom format
"2025/03/15 10:30:00"      // ✅ Slash format
```

## FlexibleBoolConverter

Tolerant boolean conversion for various frontend representations:

```json
true      // ✅
false     // ✅
1         // ✅ → true
0         // ✅ → false
"true"    // ✅ → true
"1"       // ✅ → true
```

## Registration

Global JSON config is registered in `Program.cs`:

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    var defaults = JsonConfiguration.GetDefaultOptions();
    foreach (var converter in defaults.Converters)
        options.SerializerOptions.Converters.Add(converter);
    options.SerializerOptions.PropertyNamingPolicy = defaults.PropertyNamingPolicy;
});
```

All Minimal API request deserialization and response serialization automatically uses this configuration.
