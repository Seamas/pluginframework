namespace PluginFramework.Core.Abstractions;

/// <summary>
/// 代表一个功能操作，对应 UserControl 内部的按钮、右键菜单等。
/// 权限通过操作码(permission key)控制，格式: "ModuleCode:FeatureCode:OperationCode"
/// </summary>
public interface IOperation
{
    /// <summary>操作唯一标识</summary>
    string Code { get; }

    /// <summary>操作显示名称</summary>
    string Name { get; }

    /// <summary>操作描述</summary>
    string? Description { get; }
}