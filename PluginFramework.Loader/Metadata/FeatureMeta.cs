using PluginFramework.Core.Abstractions;

namespace PluginFramework.Loader.Metadata;

/// <summary>
/// 功能的元数据，对应被 [Feature] 标记的 ViewModel 及其关联的 UserControl。
/// 可见性由下属 Operation 的权限 key 级联决定。
/// </summary>
public class FeatureMeta : IFeature
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Type ViewModelType { get; init; } = null!;
    public Type ViewType { get; init; } = null!;
    public ICollection<IOperation> Operations { get; init; } = new List<IOperation>();
}