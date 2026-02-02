using Art.Domain.Constants;
using Art.Infra.Framework.Routes;
using System.Reflection;

namespace Art.Api.Routes.Common;

/// <summary>
/// 字典路由 - 提供枚举字典接口
/// </summary>
public class DictRouter : ICommonRouterBase
{
    /// <summary>
    /// 枚举所在的程序集命名空间前缀
    /// </summary>
    private const string EnumNamespacePrefix = "Art.Domain.Enums";

    public void AddRoutes(RouteGroupBuilder group)
    {
        var dict = group.MapGroup("dict")
            .WithGroupName(ApiGroups.Common)
            .WithTags("字典");

        dict.MapGet("enums", GetEnumOptions)
            .WithSummary("获取枚举选项列表")
            .WithDescription("根据枚举名称获取枚举的所有选项，返回 value（枚举名）和 label（枚举名/注释）");
    }

    /// <summary>
    /// 获取枚举选项列表
    /// </summary>
    /// <param name="name">枚举名称，如 ActiveStatus</param>
    /// <returns>枚举选项列表</returns>
    private static IResult GetEnumOptions(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Results.BadRequest(new { code = "INVALID_PARAM", msg = "枚举名称不能为空" });
        }

        // 在 Art.Domain 程序集中查找枚举
        var assembly = Assembly.Load("Art.Domain");
        var fullTypeName = $"{EnumNamespacePrefix}.{name}";
        var enumType = assembly.GetType(fullTypeName);

        if (enumType == null || !enumType.IsEnum)
        {
            return Results.NotFound(new { code = "ENUM_NOT_FOUND", msg = $"未找到枚举: {name}" });
        }

        var options = new List<EnumOption>();
        foreach (var value in Enum.GetValues(enumType))
        {
            var enumName = value.ToString()!;
            var memberInfo = enumType.GetMember(enumName).FirstOrDefault();

            // 尝试获取 XML 注释（通过 DescriptionAttribute 或直接用枚举名）
            var label = enumName;
            var descAttr = memberInfo?.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            if (descAttr != null)
            {
                label = descAttr.Description;
            }

            options.Add(new EnumOption
            {
                Value = Convert.ToInt32(value),  // 返回枚举的数值
                Label = label
            });
        }

        return Results.Ok(options);
    }

    /// <summary>
    /// 枚举选项
    /// </summary>
    private class EnumOption
    {
        /// <summary>
        /// 枚举值（数值）
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// 显示标签
        /// </summary>
        public string Label { get; set; } = string.Empty;
    }
}
