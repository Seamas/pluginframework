namespace PluginFramework.Core.Attributes;

/// <summary>
/// 标记 ViewModel 上的 ICommand 属性为一个操作（Operation），
/// 对应 UserControl 内部的按钮、右键菜单等可执行动作
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, Inherited = false)]
public class OperationAttribute : Attribute
{
    /// <summary>操作唯一标识</summary>
    public string Code { get; }

    /// <summary>操作显示名称</summary>
    public required string Name { get; init; }

    /// <summary>操作描述</summary>
    public string? Description { get; init; }

    /// <summary>执行此操作需要的角色，为空表示无权限限制</summary>
    public string[]? RequiredRoles { get; init; }

    public OperationAttribute(string code)
    {
        Code = code;
    }
}