namespace PluginFramework.Core.Abstractions;

/// <summary>
/// 代表一个可插拔模块，通常对应一个 DLL/Assembly，
/// 在 Toolbar 上渲染为一个按钮组
/// </summary>
public interface IModule
{
    /// <summary>模块唯一标识</summary>
    string Code { get; }

    /// <summary>模块显示名称（按钮组标题）</summary>
    string Name { get; }

    /// <summary>模块描述</summary>
    string? Description { get; }

    /// <summary>此模块需要的角色，为空表示无权限限制</summary>
    string[]? RequiredRoles { get; }

    /// <summary>该模块下包含的功能列表</summary>
    ICollection<IFeature> Features { get; }
}