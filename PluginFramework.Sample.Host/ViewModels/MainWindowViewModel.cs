using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PluginFramework.Loader;
using PluginFramework.Loader.Metadata;
using PluginFramework.Sample.Host.Controls;

namespace PluginFramework.Sample.Host.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly DynamicServiceContainer _container;
    private readonly List<RelayCommand> _switchCommands = new();

    public UserControl? CurrentControl
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            foreach (var cmd in _switchCommands)
                cmd.NotifyCanExecuteChanged();
        }
    }

    [ObservableProperty]
    private FeatureMetaData? _selectedFeature;

    /// <summary>
    /// Toolbar 上的模块按钮组
    /// </summary>
    public ObservableCollection<ModuleGroupData> ModuleGroups { get; } = new();

    public MainWindowViewModel(DynamicServiceContainer container)
    {
        _container = container;
    }

    /// <summary>
    /// 用已加载的模块数据初始化 Toolbar
    /// </summary>
    public void InitializeFromModules(IReadOnlyList<PluginFramework.Loader.Metadata.ModuleMeta> modules)
    {
        ModuleGroups.Clear();
        _switchCommands.Clear();

        foreach (var module in modules)
        {
            var group = new ModuleGroupData
            {
                Code = module.Code,
                Name = module.Name
            };

            foreach (var featureMeta in module.Features.Cast<FeatureMeta>())
            {
                var featureData = new FeatureMetaData(featureMeta, module.Code);
                var cmd = new RelayCommand(
                    () => SwitchToFeature(featureData),
                    () => CurrentControl?.GetType() != featureData.ControlType);
                featureData.SwitchCommand = cmd;
                _switchCommands.Add(cmd);
                group.Features.Add(featureData);
            }

            if (group.Features.Count > 0)
                ModuleGroups.Add(group);
        }

        // 默认显示第一个 Feature
        var firstFeature = ModuleGroups
            .SelectMany(g => g.Features)
            .FirstOrDefault();

        if (firstFeature != null)
            SwitchToFeature(firstFeature);
        else
            CurrentControl = new MyDefaultControl();
    }

    private void SwitchToFeature(FeatureMetaData featureData)
    {
        if (CurrentControl?.GetType() == featureData.ControlType)
            return;

        SelectedFeature = featureData;

        // 通过 DI 容器创建 ViewModel（支持构造函数注入）
        var viewModel = _container.GetService(featureData.ViewModelType);
        if (viewModel == null)
        {
            viewModel = Activator.CreateInstance(featureData.ViewModelType);
        }

        // 创建 UserControl 并绑定 ViewModel
        var view = (UserControl?)Activator.CreateInstance(featureData.ControlType);
        if (view != null && viewModel != null)
        {
            view.DataContext = viewModel;

            // 根据权限控制 Operation 按钮的可见性
            ApplyOperationVisibility(view, featureData);
        }

        CurrentControl = view;
    }

    /// <summary>
    /// 根据权限过滤，隐藏/禁用无权限的操作按钮
    /// </summary>
    private static void ApplyOperationVisibility(UserControl view, FeatureMetaData featureData)
    {
        var allowedOpCodes = featureData.Meta.Operations
            .Select(o => o.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 遍历 UserControl 的所有子元素
        foreach (var child in FindVisualChildren<FrameworkElement>(view))
        {
            // 检查 Tag 属性中是否标记了 OperationCode
            if (child.Tag is string operationCode)
            {
                child.Visibility = allowedOpCodes.Contains(operationCode)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                yield return typedChild;

            foreach (var nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }
}
