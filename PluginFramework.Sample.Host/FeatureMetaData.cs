using System.Windows.Input;

namespace PluginFramework.Sample.Host;

public class FeatureMetaData
{
    public required string Name { get; init; }
    public required Type ControlType { get; init; }
    public ICommand? SwitchCommand { get; set; }
}