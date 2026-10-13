namespace PluginFramework.Core.Abstractions;

/// <summary>
/// 代表一个功能页面，对应一个 ViewModel + UserControl 对，
/// 在 Toolbar 上渲染为一个按钮，点击后切换内容区。
/// 可见性由下属 Operation 的权限自动级联决定。
/// </summary>
public interface IFeature
{
    /// <summary>功能唯一标识</summary>
    string Code { get; }

    /// <summary>功能显示名称</summary>
    string Name { get; }

    /// <summary>功能描述</summary>
    string? Description { get; }

    /// <summary>ViewModel 类型</summary>
    Type ViewModelType { get; }

    /// <summary>UserControl 类型</summary>
    Type ViewType { get; }

    /// <summary>该功能下包含的操作列表</summary>
    ICollection<IOperation> Operations { get; }
}