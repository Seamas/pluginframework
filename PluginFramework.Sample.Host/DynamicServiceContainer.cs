using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace PluginFramework.Sample.Host;

public class DynamicServiceContainer
{
    private readonly IServiceProvider _rootProvider;

    // 注册服务的类型， 类型描述列表
    private readonly Dictionary<Type, List<ServiceDescriptor>> _dynamicServices = new();

    // 已经注册的类型
    private readonly Dictionary<(Type serviceType, Type implementationType, ServiceLifetime lifetime), bool>
        _registrationIndex = new ();
    
    private readonly Dictionary<Type, object> _singletonCache = new();
    
    private readonly Dictionary<Type, ConstructorInfo> _constructorCache = new();
    
    private readonly HashSet<Type> _resolvingTypes = new();
    private readonly Lock _lock = new();

    public DynamicServiceContainer(IServiceProvider rootProvider)
    {
        _rootProvider = rootProvider;
    }


    #region 向动态容器注册服务, 尽量精简方法，只提供必须的方法，减少重载方法
    
    public void RegisterTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        Register<TService,TImplementation>(ServiceLifetime.Transient);
    }

    public void RegisterSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        Register<TService,TImplementation>(ServiceLifetime.Singleton);
    }

    public void RegisterSingleton<TService>(object instance) where TService : class
    {
        var serviceType = typeof(TService);
        lock (_lock)
        {
            if (!_dynamicServices.TryGetValue(typeof(TService), out var descriptors))
            {
                descriptors = new List<ServiceDescriptor>();
                _dynamicServices[serviceType] = descriptors;
            }
            var descriptor = ServiceDescriptor.Singleton(serviceType, instance);
            descriptors.Add(descriptor);
        }
    }
    
    #endregion


    #region 获取服务实例

    /// <summary>
    /// 获取一个实例
    /// 如果有多个，返回最后一个注册的服务
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <returns></returns>
    public TService? GetService<TService>() where TService : class
    {
        lock (_lock)
        {
            if (_dynamicServices.TryGetValue(typeof(TService), out var descriptors))
            {
                var descriptor = descriptors.Last();
                return (TService) GetServiceInstance(descriptor)!;
            }
        }
        
        return _rootProvider.GetService<TService>();
    }

    public IEnumerable<TService> GetServices<TService>() where TService : class
    {
        var serviceType = typeof(TService);
        var results = new List<TService>();

        lock (_lock)
        {
            foreach (var service in _rootProvider.GetServices<TService>())
            {
                results.Add(service);
            }

            if (_dynamicServices.TryGetValue(serviceType, out var descriptors))
            {
                foreach (var descriptor in descriptors)
                {
                    results.Add((TService)GetServiceInstance(descriptor)!);
                }
            }
        }

        return results;
    }

    public TService GetRequiredService<TService>() where TService : class
    {
        return GetService<TService>() ?? throw new InvalidOperationException($"Service {typeof(TService)} not found");
    }
    
    #endregion


    #region 私有方法

    private object? GetServiceInstance(ServiceDescriptor descriptor)
    {
        lock (_lock)
        {
            if (descriptor.ImplementationType == null)
            {
                return descriptor.ImplementationInstance;
            }

            if (_singletonCache.TryGetValue(descriptor.ImplementationType, out var value))
            {
                return value;
            }
            return CreateInstance(descriptor);
        }
    }

    private void Register<TService, TImplementation>(ServiceLifetime serviceLifetime) where TService : class
        where TImplementation : class, TService
    {
        var serviceType = typeof(TService);
        var implType = typeof(TImplementation);
        var key = (serviceType, implType, serviceLifetime);
        lock (_lock)
        {
            if (_registrationIndex.ContainsKey(key))
            {
                return;
            }
            
            if (!_dynamicServices.TryGetValue(typeof(TService), out var descriptors))
            {
                descriptors = new List<ServiceDescriptor>();
                _dynamicServices[serviceType] = descriptors;
            }
            var descriptor = serviceLifetime == ServiceLifetime.Transient 
                ? ServiceDescriptor.Transient(serviceType, implType)
                : ServiceDescriptor.Singleton(serviceType, implType);
            
            descriptors.Add(descriptor);
            _registrationIndex.Add(key, true);
            _constructorCache.Clear();
        }
    }
    
    
    private object? CreateInstance(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationType == null)
        {
            return descriptor.ImplementationInstance;
        }

        lock (_lock)
        {
            // 循环依赖判断
            if (_resolvingTypes.Contains(descriptor.ImplementationType))
            {
                throw new InvalidOperationException($"Circular dependency detected for {descriptor.ImplementationType.Name}");
            }
        }

        ConstructorInfo? constructor;

        lock (_lock)
        {
            if (!_constructorCache.TryGetValue(descriptor.ImplementationType, out constructor))
            {
                constructor = FindBestMatchingConstructor(descriptor.ImplementationType);
                if (constructor != null)
                {
                    _constructorCache[descriptor.ImplementationType] = constructor;
                }
            }
        }

        if (constructor == null)
            return null;

        var parameters = new List<object?>();
        
        lock (_lock)
        {
            _resolvingTypes.Add(descriptor.ImplementationType);
        }

        try
        {
            foreach (var param in constructor.GetParameters())
            {
                var paramService = ResolveDependency(param.ParameterType);
                parameters.Add(paramService);
            }

            return constructor.Invoke(parameters.ToArray());
        }
        finally
        {
            lock (_lock)
            {
                _resolvingTypes.Remove(descriptor.ImplementationType);
            }
        }
    }

    /// <summary>
    /// 查找最佳构造方法
    /// </summary>
    /// <param name="implementationType"></param>
    /// <returns></returns>
    private ConstructorInfo? FindBestMatchingConstructor(Type implementationType)
    {
        var constructors = implementationType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .ToList();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var canResolve = true;

            foreach (var param in parameters)
            {
                var canResolveParam = CanResolveDependency(param.ParameterType);
                if (!canResolveParam)
                {
                    canResolve = false;
                    break;
                }
            }

            if (canResolve)
            {
                return constructor;
            }
        }
        return constructors.FirstOrDefault();
    }

    /// <summary>
    /// 判断能够处理类型
    /// 先检查自己能否处理，如果不能，再向上级请求
    /// </summary>
    /// <param name="serviceType"></param>
    /// <returns></returns>
    private bool CanResolveDependency(Type serviceType)
    {
        lock (_lock)
        {
            if (_dynamicServices.TryGetValue(serviceType, out _))
            {
                return true;
            }
        }

        return _rootProvider.GetService(serviceType) != null;
    }

    /// <summary>
    /// 处理类型
    /// 先自己处理
    /// 如果不能，再向上级请求
    /// </summary>
    /// <param name="serviceType"></param>
    /// <returns></returns>
    private object? ResolveDependency(Type serviceType)
    {
        lock (_lock)
        {
            if (_dynamicServices.TryGetValue(serviceType, out var descriptors))
            {
                var descriptor = descriptors.Last();
                if (descriptor.Lifetime == ServiceLifetime.Singleton)
                {
                    if (descriptor.ImplementationType == null)
                    {
                        return descriptor.ImplementationInstance;
                    }
                    
                    if (!_singletonCache.TryGetValue(descriptor.ImplementationType, out var value))
                    {
                        value = CreateInstance(descriptor);
                        _singletonCache[descriptor.ImplementationType] = value!;
                    }
                    return _singletonCache[descriptor.ImplementationType];
                }
                
                return CreateInstance(descriptor);
            }
        }

        return _rootProvider.GetService(serviceType);
    }
    
    
    #endregion
}
