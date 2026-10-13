namespace PluginFramework.Core.Attributes;

/// <summary>
/// 标记 ViewModel 为一个功能页面（Feature），
/// 扫描器会自动发现并构建 FeatureMeta。
/// 权限由下属 Operation 的权限 key 级联决定。
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

    public FeatureAttribute(string code)
    {
        Code = code;
    }
}