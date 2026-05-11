using System.Collections.ObjectModel;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PluginFramework.Sample.Host.Controls;

namespace PluginFramework.Sample.Host.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly DynamicServiceContainer _dynamicContainer;
    private List<RelayCommand> _commands = new();

    private UserControl? _currentControl;

    public UserControl? CurrentControl
    {
        get;
        set
        {
            if (!SetProperty(ref field, value)) return;
            
            foreach (var command in _commands)
            {
                command.NotifyCanExecuteChanged();
            }
        }
    }

    [ObservableProperty]
    private FeatureMetaData? _selectedFeature;

    public ObservableCollection<FeatureMetaData> Features { get; } = new();

    public MainWindowViewModel(DynamicServiceContainer dynamicContainer)
    {
        _dynamicContainer = dynamicContainer;
    }

    public void InitializeFeatures()
    {
        Features.Clear();
        _commands.Clear();

        var dynamicFeatures = _dynamicContainer.GetServices<FeatureMetaData>().ToList();
        
        foreach (var feature in dynamicFeatures)
        {
            Features.Add(feature);
        }

        foreach (var feature in Features)
        {
            var command = new RelayCommand(() => SwitchControl(feature), () => CurrentControl?.GetType() != feature.ControlType);
            feature.SwitchCommand = command;
            _commands.Add(command);
        }

        CurrentControl = CreateControl(typeof(MyDefaultControl));
    }

    private void SwitchControl(FeatureMetaData feature)
    {
        if (CurrentControl?.GetType() == feature.ControlType)
        {
            return;
        }

        SelectedFeature = feature;
        CurrentControl = CreateControl(feature.ControlType);
    }

    private UserControl CreateControl(Type type)
    {
        return (UserControl)Activator.CreateInstance(type)!;
    }
}
