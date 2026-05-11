using System.Windows;
using PluginFramework.Sample.Host.ViewModels;

namespace PluginFramework.Sample.Host;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        
        DataContext = viewModel;
    }
}