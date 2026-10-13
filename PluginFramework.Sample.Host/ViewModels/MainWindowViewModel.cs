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
    public void InitializeFromModules(IReadOnlyList<ModuleMeta> modules)
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

            // Operations 已在 DefaultModuleLoader 中按权限 key 过滤，
            // 到达此处时 featureData.Meta.Operations 仅包含有权执行的操作。
            // UI 可通过绑定到 ViewModel 的权限判断方法自行控制按钮显隐。
        }

        CurrentControl = view;
    }
}
