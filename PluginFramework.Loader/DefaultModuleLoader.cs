using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PluginFramework.Core;
using PluginFramework.Loader.Metadata;

namespace PluginFramework.Loader;

/// <summary>
/// 默认模块加载器实现。
/// 权限过滤基于 operation-key：用户拥有任何操作权限即自动级联显示 Feature 和 Module。
/// </summary>
public class DefaultModuleLoader : IModuleLoader
{
    private readonly DynamicServiceContainer _container;
    private readonly ModuleScanner _scanner;

    public DefaultModuleLoader(DynamicServiceContainer container, ModuleScanner scanner)
    {
        _container = container;
        _scanner = scanner;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ModuleMeta>> LoadModulesAsync(
        string modulesDirectory, HashSet<string> userPermissionKeys)
    {
        if (!Directory.Exists(modulesDirectory))
            return Task.FromResult<IReadOnlyList<ModuleMeta>>(Array.Empty<ModuleMeta>());

        var allModules = new List<ModuleMeta>();
        var dllFiles = Directory.GetFiles(modulesDirectory, "*.dll", SearchOption.TopDirectoryOnly);

        foreach (var dllPath in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                var moduleMetas = _scanner.ScanAssembly(assembly);

                foreach (var module in moduleMetas)
                {
                    // 1. 检查模块级权限（是否有任何 Feature 可访问）
                    if (!PermissionKeyHelper.HasModulePermission(userPermissionKeys, module.Code))
                        continue;

                    // 2. 调用 ConfigureServices 注册模块的 DI 服务
                    var tempServices = new ServiceCollection();
                    if (Activator.CreateInstance(module.ConfigureType) is IModuleConfigure configure)
                    {
                        configure.ConfigureServices(tempServices);
                    }

                    // 3. 扫描到的 ViewModel 自动注册到 DI
                    foreach (var feature in module.Features)
                    {
                        tempServices.AddTransient(feature.ViewModelType, feature.ViewModelType);
                    }

                    // 4. 将模块的 DI 注册转移到 DynamicServiceContainer
                    TransferToContainer(tempServices);

                    // 5. 过滤 Feature（按权限 key 级联）
                    var allowedFeatures = new List<FeatureMeta>();
                    foreach (var f in module.Features.Cast<FeatureMeta>())
                    {
                        if (!PermissionKeyHelper.HasFeaturePermission(userPermissionKeys, module.Code, f.Code))
                            continue;

                        // 6. 过滤 Operation（按权限 key）
                        var allowedOps = f.Operations
                            .Cast<OperationMeta>()
                            .Where(o => PermissionKeyHelper.HasOperationPermission(
                                userPermissionKeys, module.Code, f.Code, o.Code))
                            .ToList();

                        f.Operations.Clear();
                        foreach (var op in allowedOps) f.Operations.Add(op);

                        allowedFeatures.Add(f);
                    }

                    if (allowedFeatures.Count > 0)
                    {
                        module.Features.Clear();
                        foreach (var f in allowedFeatures) module.Features.Add(f);
                        allModules.Add(module);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load module {dllPath}: {ex.Message}");
            }
        }

        return Task.FromResult<IReadOnlyList<ModuleMeta>>(allModules);
    }

    private void TransferToContainer(ServiceCollection tempServices)
    {
        foreach (var descriptor in tempServices)
        {
            if (descriptor.ImplementationType != null)
            {
                switch (descriptor.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        _container.RegisterSingleton(descriptor.ServiceType, descriptor.ImplementationType);
                        break;
                    case ServiceLifetime.Transient:
                    case ServiceLifetime.Scoped:
                        _container.RegisterTransient(descriptor.ServiceType, descriptor.ImplementationType);
                        break;
                }
            }
            else if (descriptor.ImplementationInstance != null)
            {
                _container.RegisterSingleton(descriptor.ServiceType, descriptor.ImplementationInstance);
            }
        }
    }
}