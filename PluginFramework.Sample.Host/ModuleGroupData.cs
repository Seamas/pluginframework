using System.Collections.ObjectModel;

namespace PluginFramework.Sample.Host;

/// <summary>
/// Toolbar 上的模块按钮组数据
/// </summary>
public class ModuleGroupData
{
    /// <summary>模块显示名称</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>模块编码</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>该模块下的功能列表</summary>
    public ObservableCollection<FeatureMetaData> Features { get; init; } = new();
}