using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Art.Infra.Extensions;

public static partial class EntityFrameworkExtensions
{
    // 静态包装方法，确保表达式树兼容性
    private static string SerializeToJson<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }

    private static T? DeserializeFromJson<T>(string value) where T : class, new()
    {
        try
        {
            return JsonSerializer.Deserialize<T>(value) ?? new T();
        }
        catch
        {
            // 如果 JSON 格式错误，返回 null 而不是抛异常
            return null;
        }
    }

    /// <summary>
    /// 为实体属性配置 JSON 序列化转换（非空类型）
    /// </summary>
    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> propertyBuilder) where T : class, new()
    {
        ValueConverter<T, string> converter = new ValueConverter<T, string>
        (
            v => SerializeToJson(v),
            v => DeserializeFromJson<T>(v) ?? new T()
        );
        ValueComparer<T> comparer = new ValueComparer<T>
        (
            (l, r) => SerializeToJson(l) == SerializeToJson(r),
            v => v == null ? 0 : SerializeToJson(v).GetHashCode(),
            v => DeserializeFromJson<T>(SerializeToJson(v)) ?? new T()
        );
        propertyBuilder.HasConversion(converter);
        propertyBuilder.Metadata.SetValueConverter(converter);
        propertyBuilder.Metadata.SetValueComparer(comparer);
        return propertyBuilder;
    }

    /// <summary>
    /// 为实体属性配置 JSON 序列化转换（可空类型）
    /// </summary>
    public static PropertyBuilder<T?> HasJsonConversionNullable<T>(this PropertyBuilder<T?> propertyBuilder) where T : class, new()
    {
        var converter = new ValueConverter<T, string>
        (
            v => SerializeToJson(v),
            v => DeserializeFromJson<T>(v) ?? new T()
        );
        var comparer = new ValueComparer<T>
        (
            (l, r) => SerializeToJson(l) == SerializeToJson(r),
            v => v == null ? 0 : SerializeToJson(v).GetHashCode(),
            v => DeserializeFromJson<T>(SerializeToJson(v)) ?? new T()
        );
        propertyBuilder.HasConversion(converter!);
        propertyBuilder.Metadata.SetValueConverter(converter);
        propertyBuilder.Metadata.SetValueComparer(comparer);
        return propertyBuilder;
    }
}
