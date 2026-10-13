using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PluginFramework.Core.Attributes;

namespace PluginFramework.Sample.Module;

/// <summary>
/// 示例 Feature：通过 [Feature] 标记，自动被扫描器发现
/// View 按约定查找：MyCustomControlViewModel → MyCustomControl（去掉 "ViewModel" 后缀）
/// </summary>
[Feature("myCustom", Name = "自定义功能")]
public partial class MyCustomControlViewModel : ObservableObject
{
    [ObservableProperty]
    private string _message = "欢迎使用自定义功能";

    /// <summary>
/// [Operation] 标记的命令会被扫描为 IOperation，
/// 权限 key = "sample:myCustom:refresh"
/// </summary>
[Operation("refresh", Name = "刷新数据")]
[RelayCommand]
private void Refresh()
{
    Message = $"数据已刷新: {DateTime.Now:HH:mm:ss}";
}

/// <summary>
/// 权限 key = "sample:myCustom:adminAction"
/// 只有拥有此 key 的用户才能看到并执行
/// </summary>
[Operation("adminAction", Name = "管理员操作")]
    private void AdminAction()
    {
        Message = "管理员操作已执行";
    }

    /// <summary>
    /// 普通命令，不加 [Operation] 不会被扫描
    /// </summary>
    [RelayCommand]
    private void Clear()
    {
        Message = string.Empty;
    }
}