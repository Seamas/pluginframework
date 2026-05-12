using System.Reflection;
using System.Windows.Input;
using PluginFramework.Core;
using PluginFramework.Core.Abstractions;
using PluginFramework.Core.Attributes;
using PluginFramework.Loader.Metadata;

namespace PluginFramework.Loader;

/// <summary>
/// 模块扫描器：从 Assembly 中扫描 Module → Feature → Operation 三层元数据
/// </summary>
public class ModuleScanner
{
    /// <summary>
    /// View 类型解析器委托。输入 ViewModel 类型和 Feature 特性，输出对应的 View 类型。
    /// 如果返回 null，该 Feature 将被跳过。
    /// </summary>
    public Func<Assembly, Type, FeatureAttribute, Type?>? ViewResolver { get; init; }

    /// <summary>
    /// 扫描一个 Assembly，构建 ModuleMeta（含 Features 和 Operations）
    /// </summary>
    /// <param name="assembly">要扫描的程序集</param>
    /// <returns>扫描到的模块元数据列表（一个 Assembly 可能包含多个 Module）</returns>
    public IReadOnlyList<ModuleMeta> ScanAssembly(Assembly assembly)
    {
        var modules = new List<ModuleMeta>();

        // 1. 找到所有带 [ModuleMeta] 的 IModuleConfigure 实现类
        var configureTypes = assembly.GetExportedTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true }
                        && typeof(IModuleConfigure).IsAssignableFrom(t)
                        && t.GetCustomAttribute<ModuleMetaAttribute>() is not null);

        foreach (var configureType in configureTypes)
        {
            var moduleAttr = configureType.GetCustomAttribute<ModuleMetaAttribute>()!;
            var module = new ModuleMeta
            {
                Code = moduleAttr.Code,
                Name = moduleAttr.Name,
                Description = moduleAttr.Description,
                RequiredRoles = moduleAttr.RequiredRoles,
                Assembly = assembly,
                ConfigureType = configureType
            };

            // 2. 扫描该 Assembly 中所有带 [Feature] 的 ViewModel 类
            var featureTypes = assembly.GetExportedTypes()
                .Where(t => t is { IsAbstract: false, IsClass: true }
                            && t.GetCustomAttribute<FeatureAttribute>() is not null);

            foreach (var viewModelType in featureTypes)
            {
                var featureAttr = viewModelType.GetCustomAttribute<FeatureAttribute>()!;
                var viewType = ResolveViewType(assembly, viewModelType, featureAttr);

                if (viewType == null)
                {
                    // 找不到对应的 View，跳过（可在日志中记录警告）
                    continue;
                }

                var feature = new FeatureMeta
                {
                    Code = featureAttr.Code,
                    Name = featureAttr.Name,
                    Description = featureAttr.Description,
                    ViewModelType = viewModelType,
                    ViewType = viewType,
                    RequiredRoles = featureAttr.RequiredRoles
                };

                // 3. 扫描 ViewModel 上带 [Operation] 的 ICommand 属性
                var operations = ScanOperations(viewModelType);
                foreach (var op in operations)
                {
                    feature.Operations.Add(op);
                }

                module.Features.Add(feature);
            }

            modules.Add(module);
        }

        return modules;
    }

    /// <summary>
    /// 扫描 ViewModel 类上所有带 [Operation] 特性的 ICommand 属性或 [RelayCommand] 方法
    /// </summary>
    public static List<OperationMeta> ScanOperations(Type viewModelType)
    {
        var operations = new List<OperationMeta>();

        // 1. 扫描 ICommand 属性上的 [Operation]
        var commandProperties = viewModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => typeof(ICommand).IsAssignableFrom(p.PropertyType)
                        && p.GetCustomAttribute<OperationAttribute>() is not null);

        foreach (var prop in commandProperties)
        {
            var opAttr = prop.GetCustomAttribute<OperationAttribute>()!;
            operations.Add(new OperationMeta
            {
                Code = opAttr.Code,
                Name = opAttr.Name,
                Description = opAttr.Description,
                RequiredRoles = opAttr.RequiredRoles,
                CommandProperty = prop
            });
        }

        // 2. 扫描方法上的 [Operation]（配合 CommunityToolkit.Mvvm 的 [RelayCommand]）
        //    [RelayCommand] 方法 Refresh() → 生成 RefreshCommand 属性
        var operationMethods = viewModelType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<OperationAttribute>() is not null);

        foreach (var method in operationMethods)
        {
            var opAttr = method.GetCustomAttribute<OperationAttribute>()!;
            var commandPropName = $"{method.Name}Command";
            var commandProp = viewModelType.GetProperty(commandPropName,
                BindingFlags.Public | BindingFlags.Instance);

            // 只添加能找到对应 ICommand 属性的 Operation
            if (commandProp != null && typeof(ICommand).IsAssignableFrom(commandProp.PropertyType))
            {
                operations.Add(new OperationMeta
                {
                    Code = opAttr.Code,
                    Name = opAttr.Name,
                    Description = opAttr.Description,
                    RequiredRoles = opAttr.RequiredRoles,
                    CommandProperty = commandProp
                });
            }
        }

        return operations;
    }

    /// <summary>
    /// 解析 ViewModel 对应的 View 类型
    /// 优先使用注入的 ViewResolver，其次使用 FeatureAttribute 上显式指定的 ViewType
    /// </summary>
    private Type? ResolveViewType(Assembly assembly, Type viewModelType, FeatureAttribute featureAttr)
    {
        // 1. 显式指定了 ViewType
        if (featureAttr.ViewType != null)
        {
            return featureAttr.ViewType;
        }

        // 2. 使用注入的 ViewResolver
        if (ViewResolver != null)
        {
            return ViewResolver(assembly, viewModelType, featureAttr);
        }

        // 3. 都没有则返回 null，该 Feature 被跳过
        return null;
    }
}