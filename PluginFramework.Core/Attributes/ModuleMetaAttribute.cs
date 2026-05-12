namespace PluginFramework.Core.Attributes;

/// <summary>
/// 标记模块配置类（IModuleConfigure 实现），
/// 扫描器会自动发现并构建 ModuleMeta
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ModuleMetaAttribute(string code) : Attribute
{
    /// <summary>模块唯一标识</summary>
    public string Code { get; } = code;

    /// <summary>模块显示名称</summary>
    public required string Name { get; init; }

    /// <summary>模块描述</summary>
    public string? Description { get; init; }

    /// <summary>访问此模块需要的角色，为空表示无权限限制</summary>
    public string[]? RequiredRoles { get; init; }
}