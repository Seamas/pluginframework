using System.Reflection;
using PluginFramework.Core.Abstractions;

namespace PluginFramework.Loader.Metadata;

/// <summary>
/// 操作的元数据，对应 ViewModel 上被 [Operation] 标记的 ICommand 属性
/// </summary>
public class OperationMeta : IOperation
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string[]? RequiredRoles { get; init; }

    /// <summary>ViewModel 上对应的 PropertyInfo，用于运行时获取 ICommand 实例</summary>
    public PropertyInfo? CommandProperty { get; init; }
}