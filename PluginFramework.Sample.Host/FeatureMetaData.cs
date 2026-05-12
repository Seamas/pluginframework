using System.Windows.Input;
using PluginFramework.Loader.Metadata;

namespace PluginFramework.Sample.Host;

/// <summary>
/// 用于 UI 绑定的 Feature 数据包装类，
/// 在 FeatureMeta 基础上增加 SwitchCommand 用于 Toolbar 按钮绑定
/// </summary>
public class FeatureMetaData
{
    /// <summary>底层元数据</summary>
    public FeatureMeta Meta { get; }

    /// <summary>显示名称</summary>
    public string Name => Meta.Name;

    /// <summary>对应的 UserControl 类型</summary>
    public Type ControlType => Meta.ViewType;

    /// <summary>ViewModel 类型</summary>
    public Type ViewModelType => Meta.ViewModelType;

    /// <summary>所属模块编码</summary>
    public string ModuleCode { get; }

    /// <summary>切换到此 Feature 的命令（由 MainWindowViewModel 设置）</summary>
    public ICommand? SwitchCommand { get; set; }

    public FeatureMetaData(FeatureMeta meta, string moduleCode)
    {
        Meta = meta;
        ModuleCode = moduleCode;
    }
}