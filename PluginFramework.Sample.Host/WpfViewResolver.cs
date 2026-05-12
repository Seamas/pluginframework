using System.Reflection;
using System.Windows.Controls;
using PluginFramework.Core.Attributes;

namespace PluginFramework.Sample.Host;

/// <summary>
/// WPF 平台的 View 类型解析器
/// 约定：{Name}ViewModel → {Name}View 或 {Name}Control
/// 只在包含 UserControl 的类型中查找
/// </summary>
public static class WpfViewResolver
{
    /// <summary>
    /// 按约定解析 ViewModel 对应的 UserControl 类型
    /// </summary>
    public static Type? Resolve(Assembly assembly, Type viewModelType, FeatureAttribute featureAttr)
    {
        // 显式指定优先
        if (featureAttr.ViewType != null)
            return featureAttr.ViewType;

        var vmName = viewModelType.Name;
        string baseName;

        if (vmName.EndsWith("ViewModel", StringComparison.Ordinal))
            baseName = vmName[..^"ViewModel".Length];
        else
            baseName = vmName;

        var candidateNames = new[] { $"{baseName}View", $"{baseName}Control", baseName };

        foreach (var candidateName in candidateNames)
        {
            // 优先同命名空间
            var type = assembly.GetExportedTypes()
                .FirstOrDefault(t => t.Name == candidateName
                                     && t.Namespace == viewModelType.Namespace
                                     && typeof(UserControl).IsAssignableFrom(t));
            if (type != null) return type;

            // 回退全局
            type = assembly.GetExportedTypes()
                .FirstOrDefault(t => t.Name == candidateName
                                     && typeof(UserControl).IsAssignableFrom(t));
            if (type != null) return type;
        }

        return null;
    }
}