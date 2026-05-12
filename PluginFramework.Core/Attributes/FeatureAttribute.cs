namespace PluginFramework.Core.Attributes;

/// <summary>
/// 标记 ViewModel 为一个功能页面（Feature），
/// 扫描器会自动发现并构建 FeatureMeta
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class FeatureAttribute : Attribute
{
    /// <summary>功能唯一标识</summary>
    public string Code { get; }

    /// <summary>功能显示名称</summary>
    public required string Name { get; init; }

    /// <summary>功能描述</summary>
    public string? Description { get; init; }

    /// <summary>显式指定对应的 UserControl 类型，不指定则按约定查找</summary>
    public Type? ViewType { get; init; }

    /// <summary>访问此功能需要的角色，为空表示无权限限制</summary>
    public string[]? RequiredRoles { get; init; }

    public FeatureAttribute(string code)
    {
        Code = code;
    }
}