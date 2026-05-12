using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PluginFramework.Core;
using PluginFramework.Loader.Metadata;

namespace PluginFramework.Loader;

/// <summary>
/// 默认模块加载器实现
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
    public Task<IReadOnlyList<ModuleMeta>> LoadModulesAsync(string modulesDirectory, string[] userRoles)
    {
        if (!Directory.Exists(modulesDirectory))
        {
            return Task.FromResult<IReadOnlyList<ModuleMeta>>(Array.Empty<ModuleMeta>());
        }

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
                    // 1. 检查模块级权限
                    if (!HasAccess(module.RequiredRoles, userRoles))
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
                                    _container.RegisterTransient(descriptor.ServiceType, descriptor.ImplementationType);
                                    break;
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

                    // 5. 过滤 Feature（按权限）
                    var allowedFeatures = module.Features
                        .Where(f => HasAccess(f.RequiredRoles, userRoles))
                        .ToList();

                    // 6. 过滤每个 Feature 下的 Operation（按权限）
                    foreach (var feature in allowedFeatures)
                    {
                        var allowedOps = feature.Operations
                            .Where(o => HasAccess(o.RequiredRoles, userRoles))
                            .ToList();

                        feature.Operations.Clear();
                        foreach (var op in allowedOps)
                        {
                            feature.Operations.Add(op);
                        }
                    }

                    // 只保留有 Feature 的模块
                    if (allowedFeatures.Count > 0)
                    {
                        module.Features.Clear();
                        foreach (var feature in allowedFeatures)
                        {
                            module.Features.Add(feature);
                        }
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

    /// <summary>
    /// 检查用户角色是否满足权限要求
    /// </summary>
    private static bool HasAccess(string[]? requiredRoles, string[] userRoles)
    {
        if (requiredRoles == null || requiredRoles.Length == 0)
            return true;
        if (userRoles.Length == 0)
            return false;
        return requiredRoles.Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }
}