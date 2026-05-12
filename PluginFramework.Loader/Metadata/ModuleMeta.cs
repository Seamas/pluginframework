using System.Reflection;
using PluginFramework.Core.Abstractions;

namespace PluginFramework.Loader.Metadata;

/// <summary>
/// 模块的元数据，对应一个被 [ModuleMeta] 标记的 IModuleConfigure 实现类所在的 Assembly
/// </summary>
public class ModuleMeta : IModule
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string[]? RequiredRoles { get; init; }

    /// <summary>模块所在的 Assembly</summary>
    public Assembly Assembly { get; init; } = null!;

    /// <summary>IModuleConfigure 实现类型</summary>
    public Type ConfigureType { get; init; } = null!;

    public ICollection<IFeature> Features { get; init; } = new List<IFeature>();
}