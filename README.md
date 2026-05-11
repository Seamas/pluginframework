# PluginFramework - 可插拔C#客户端应用框架

## 📖 概述

PluginFramework 是一套轻量级、可扩展的C#客户端应用框架，采用模块化设计理念，支持IOC容器管理、权限体系控制、功能点管理。框架设计参考了ABP框架的核心思想，但专注于桌面客户端应用场景，提供简洁、高效的插件化解决方案。

## 🏗️ 项目架构

```
PluginFramework/
├── PluginFramework.Core/                 # 核心接口定义层
│   ├── Modules/                         # 模块系统接口
│   ├── Permissions/                     # 权限系统接口
│   ├── Features/                        # 功能系统接口
│   ├── DependencyInjection/             # 依赖注入接口
│   ├── Configuration/                  # 配置系统接口
│   ├── Application/                     # 应用层接口
│   └── Extensions/                     # 扩展方法
│
├── PluginFramework.Loader/               # 具体实现层
│   ├── Modules/                         # 模块管理器实现
│   ├── Permissions/                     # 权限检查器实现
│   ├── Features/                        # 功能检查器实现
│   ├── DependencyInjection/             # IOC容器实现
│   └── Application/                     # 应用启动器实现
│
├── PluginFramework.Sample.Module/        # 示例模块
└── PluginFramework.Sample.Host/         # 示例宿主程序
```

### 核心设计原则

1. **接口与实现分离**：Core层仅包含接口定义，Loader层提供具体实现
2. **强制约束**：每个类对应一个文件（内部类除外），确保代码结构清晰
3. **IOC驱动**：所有服务通过依赖注入进行管理
4. **模块化加载**：支持动态加载DLL作为独立模块
5. **权限过滤**：基于用户权限控制功能可见性和可用性

## 🔑 核心接口设计

### 1. 模块系统 (IModule)

模块是框架的核心组成部分，每个DLL模块必须实现`IModule`接口。框架会在应用启动时扫描并初始化所有已配置的模块，按照依赖关系排序执行。

**IModule接口定义**：

```csharp
namespace PluginFramework.Core.Modules;

public interface IModule
{
    // 声明模块依赖的其他模块类型
    Type[] Dependencies { get; }

    // 预配置阶段，用于注册服务（最先执行）
    void PreConfigureServices();

    // 配置服务阶段，用于配置已注册的服务
    void ConfigureServices();

    // 初始化完成后调用，用于业务初始化
    void PostInitialize();

    // 应用关闭时调用，用于释放资源
    void Shutdown();
}
```

**模块特性标记**：

```csharp
[Module(
    StartLevel = ModuleStartLevel.Application,
    Name = "SampleModule",
    Version = "1.0.0",
    Description = "示例模块",
    Author = "PluginFramework"
)]
public class SampleModule : ModuleBase
{
    // 模块实现
}
```

**启动等级说明**：

| 等级 | 名称 | 说明 |
|------|------|------|
| 0 | Core | 核心服务级别，最先启动 |
| 100 | Framework | 框架级别 |
| 200 | Application | 应用级别（默认） |
| 300 | Business | 业务级别 |
| 400 | Frontend | 前端级别，最后启动 |

**基础模块类**：

框架提供了`ModuleBase`基类，包含常用功能：

```csharp
public abstract class ModuleBase : IModule
{
    // 注入的服务集合
    protected internal IServiceCollection Services { get; internal set; }

    // 启动配置
    protected internal IStartupConfiguration StartupConfiguration { get; internal set; }

    // 注册配置选项
    protected void Configure<TService>(Action<TService> configure) where TService : class;

    // 获取配置选项
    protected TOptions GetOptions<TOptions>() where TOptions : class, new();
}
```

**使用示例**：

```csharp
[Module(ModuleStartLevel.Application, Name = "SampleModule", Version = "1.0.0")]
public class SampleModule : ModuleBase
{
    public override Type[] Dependencies => Array.Empty<Type>();

    public override void PreConfigureServices()
    {
        Console.WriteLine("SampleModule: PreConfigureServices");
        // 可以在此阶段注册需要提前配置的服务
    }

    public override void ConfigureServices()
    {
        Console.WriteLine("SampleModule: ConfigureServices");

        // 注册服务到IOC容器
        Services.AddSingleton<ISampleService, SampleService>();
        Services.AddSingleton<ISampleRepository, SampleRepository>();
        Services.AddTransient<SampleController>();
    }

    public override void PostInitialize()
    {
        Console.WriteLine("SampleModule: PostInitialize");
        // 在此阶段进行业务初始化，如加载缓存、验证配置等
    }

    public override void Shutdown()
    {
        Console.WriteLine("SampleModule: Shutdown");
        // 释放资源，如关闭数据库连接、保存状态等
    }
}
```

### 2. 权限系统 (IPermissionProvider)

权限系统用于控制用户对功能的访问权限。每个模块可以定义自己的权限集合，权限之间支持层级关系。

**权限提供者接口**：

```csharp
public interface IPermissionProvider
{
    void Define(IPermissionDefinitionContext context);
}
```

**定义上下文**：

```csharp
public interface IPermissionDefinitionContext
{
    // 获取或创建权限组
    IPermissionGroupDefinition GetOrCreateGroup(string name, string? displayName = null);

    // 获取或创建权限
    IPermissionDefinition GetOrCreatePermission(
        string name,
        string groupName,
        string? displayName = null,
        string? description = null,
        string? parentName = null);
}
```

**权限检查器**：

```csharp
public interface IPermissionChecker
{
    // 检查指定权限
    Task<bool> CheckAsync(string permissionName, string? userId = null);

    // 检查所有权限
    Task<bool> CheckAllAsync(IEnumerable<string> permissions);

    // 检查任意权限
    Task<bool> CheckAnyAsync(IEnumerable<string> permissions);

    // 获取所有已授权权限
    Task<IReadOnlyList<string>> GetGrantedPermissionsAsync(string? userId = null);
}
```

**权限特性标记**：

```csharp
[RequiresPermission("Sample.View")]
public class SampleController
{
    // 方法实现
}
```

**使用示例**：

```csharp
public class SamplePermissionProvider : IPermissionProvider
{
    public void Define(IPermissionDefinitionContext context)
    {
        // 创建权限组
        var sampleGroup = context.GetOrCreateGroup("Sample", "示例权限组");

        // 创建基础权限
        context.GetOrCreatePermission(
            "Sample.View",
            "Sample",
            "查看示例",
            "允许查看示例数据");

        // 创建带父子关系的权限
        context.GetOrCreatePermission(
            "Sample.Create",
            "Sample",
            "创建示例",
            "允许创建示例数据",
            parentName: "Sample.View");  // 依赖父权限

        context.GetOrCreatePermission(
            "Sample.Edit",
            "Sample",
            "编辑示例",
            "允许编辑示例数据",
            parentName: "Sample.View");

        context.GetOrCreatePermission(
            "Sample.Delete",
            "Sample",
            "删除示例",
            "允许删除示例数据",
            parentName: "Sample.View");
    }
}
```

### 3. 功能系统 (IFeatureProvider)

功能系统用于管理模块提供的功能点，可以基于功能点控制模块的加载和功能的可见性。

**功能提供者接口**：

```csharp
public interface IFeatureProvider
{
    void Define(IFeatureDefinitionContext context);
}
```

**定义上下文**：

```csharp
public interface IFeatureDefinitionContext
{
    // 获取或创建功能组
    IFeatureGroupDefinition GetOrCreateGroup(string name, string? displayName = null);

    // 获取或创建功能
    IFeatureDefinition GetOrCreateFeature(
        string name,
        string groupName,
        string? displayName = null,
        string? description = null,
        string featureType = "Standard");

    // 添加功能依赖
    void AddFeatureDependency(string featureName, string dependencyName);
}
```

**功能检查器**：

```csharp
public interface IFeatureChecker
{
    // 检查功能是否启用
    Task<bool> IsEnabledAsync(string featureName, string? providerKey = null, string? providerName = null);

    // 检查所有功能是否启用
    Task<bool> AreAllEnabledAsync(IEnumerable<string> featureNames, string? providerKey = null, string? providerName = null);

    // 获取所有启用的功能
    Task<IReadOnlyList<string>> GetEnabledFeaturesAsync(string? providerKey = null, string? providerName = null);
}
```

**使用示例**：

```csharp
public class SampleFeatureProvider : IFeatureProvider
{
    public void Define(IFeatureDefinitionContext context)
    {
        // 创建功能组
        var sampleGroup = context.GetOrCreateGroup("Sample", "示例功能组");

        // 创建基础功能
        context.GetOrCreateFeature(
            "Sample.Feature.Basic",
            "Sample",
            "基础示例功能",
            "提供基本的示例功能");

        // 创建高级功能
        context.GetOrCreateFeature(
            "Sample.Feature.Advanced",
            "Sample",
            "高级示例功能",
            "提供高级的示例功能");

        // 添加功能依赖
        context.AddFeatureDependency("Sample.Feature.Advanced", "Sample.Feature.Basic");
    }
}
```

### 4. 依赖注入系统

框架使用Microsoft.Extensions.DependencyInjection作为IOC容器，并提供了扩展特性用于自动注册服务。

**服务注册特性**：

```csharp
// 单例服务
[Singleton]
public class SampleService : ISampleService { }

// 瞬态服务
[Transient]
public class SampleController { }

// 作用域服务
[Scoped]
public class SampleRepository { }

// 指定服务类型
[Singleton(typeof(ISampleService))]
public class SampleServiceImpl : ISampleService { }
```

**约定自动注册**：

框架支持基于命名约定的自动注册：

```csharp
// 以 Service 结尾的类会自动注册为瞬态服务
public class UserService { }  // 自动注册为 IUserService

// 以 Repository 结尾的类会自动注册为单例服务
public class UserRepository { }  // 自动注册为 IUserRepository
```

**IOC管理器**：

```csharp
public interface IIocManager
{
    void Register<TService, TImplementation>() where TService : class where TImplementation : class, TService;
    bool IsRegistered<TService>();
    TService Resolve<TService>();
    TService? TryResolve<TService>();
}
```

## 🚀 应用启动

### 使用ApplicationBuilder构建应用

```csharp
// 创建应用构建器
var application = ApplicationBuilder.Create()
    .SetAppName("MyApplication")
    .SetVersion("1.0.0")
    .SetBaseDirectory(AppDomain.CurrentDomain.BaseDirectory)
    .Build();

// 配置模块
application.AddModule<SampleModule>();

// 添加插件目录
application.AddPluginPath("./Plugins");

// 设置用户信息
application.SetUser(new UserInfo
{
    UserId = "user-001",
    UserName = "TestUser",
    Roles = new List<string> { "Admin" },
    IsAdmin = false
});

// 设置启用的功能
application.SetEnabledFeatures("Sample.Feature.Basic");

// 设置授权的权限
application.SetGrantedPermissions("Sample.View", "Sample.Create");

// 配置额外服务
application.ConfigureServices(services =>
{
    services.AddSingleton<IPermissionProvider, SamplePermissionProvider>();
    services.AddSingleton<IFeatureProvider, SampleFeatureProvider>();
});

// 启动应用
await application.StartAsync();

// 使用服务
var service = application.ServiceProvider.GetService<ISampleService>();
Console.WriteLine(service.GetData());

// 关闭应用
await application.StopAsync();
```

### 完整启动流程

1. **构建应用**：创建ApplicationBuilder并配置
2. **加载模块**：扫描并实例化所有配置的模块
3. **解析依赖**：按照依赖关系排序模块
4. **PreConfigureServices**：执行各模块的预配置
5. **ConfigureServices**：执行各模块的服务注册
6. **PostInitialize**：执行各模块的业务初始化
7. **应用运行**：此时所有服务已就绪
8. **Shutdown**：按照反向顺序执行各模块的清理

## 📋 示例模块实现

完整的示例模块实现：

```csharp
[Module(ModuleStartLevel.Application, Name = "SampleModule", Version = "1.0.0", Description = "示例模块")]
public class SampleModule : ModuleBase
{
    public override Type[] Dependencies => Array.Empty<Type>();

    public override void PreConfigureServices()
    {
        Console.WriteLine("SampleModule: PreConfigureServices");
    }

    public override void ConfigureServices()
    {
        Console.WriteLine("SampleModule: ConfigureServices");

        Services.AddSingleton<ISampleService, SampleService>();
        Services.AddSingleton<ISampleRepository, SampleRepository>();
        Services.AddTransient<SampleController>();
    }

    public override void PostInitialize()
    {
        Console.WriteLine("SampleModule: PostInitialize");

        // 获取注册的服务
        var permissionProvider = Services.BuildServiceProvider().GetService<IPermissionProvider>();
        if (permissionProvider != null)
        {
            Console.WriteLine("SampleModule: PermissionProvider registered");
        }
    }

    public override void Shutdown()
    {
        Console.WriteLine("SampleModule: Shutdown");
    }
}

// 服务接口
public interface ISampleService
{
    string GetData();
}

// 服务实现
[Singleton]
public class SampleService : ISampleService
{
    private readonly ISampleRepository _repository;

    public SampleService(ISampleRepository repository)
    {
        _repository = repository;
    }

    public string GetData()
    {
        return $"Sample data from repository: {_repository.GetData()}";
    }
}

// 仓储接口
public interface ISampleRepository
{
    string GetData();
}

// 仓储实现
[Singleton]
public class SampleRepository : ISampleRepository
{
    public string GetData()
    {
        return "SampleRepository";
    }
}

// 控制器
[Transient]
public class SampleController
{
    private readonly ISampleService _sampleService;

    public SampleController(ISampleService sampleService)
    {
        _sampleService = sampleService;
    }

    public void Execute()
    {
        Console.WriteLine(_sampleService.GetData());
    }
}

// 权限提供者
public class SamplePermissionProvider : IPermissionProvider
{
    public void Define(IPermissionDefinitionContext context)
    {
        var sampleGroup = context.GetOrCreateGroup("Sample", "示例权限组");

        context.GetOrCreatePermission(
            "Sample.View", "Sample", "查看示例", "允许查看示例数据");

        context.GetOrCreatePermission(
            "Sample.Create", "Sample", "创建示例", "允许创建示例数据", "Sample.View");

        context.GetOrCreatePermission(
            "Sample.Edit", "Sample", "编辑示例", "允许编辑示例数据", "Sample.View");

        context.GetOrCreatePermission(
            "Sample.Delete", "Sample", "删除示例", "允许删除示例数据", "Sample.View");
    }
}

// 功能提供者
public class SampleFeatureProvider : IFeatureProvider
{
    public void Define(IFeatureDefinitionContext context)
    {
        var sampleGroup = context.GetOrCreateGroup("Sample", "示例功能组");

        context.GetOrCreateFeature(
            "Sample.Feature.Basic", "Sample", "基础示例功能", "提供基本的示例功能");

        context.GetOrCreateFeature(
            "Sample.Feature.Advanced", "Sample", "高级示例功能", "提供高级的示例功能");

        context.AddFeatureDependency("Sample.Feature.Advanced", "Sample.Feature.Basic");
    }
}
```

## 🔍 权限和功能检查示例

```csharp
// 检查权限
var permissionChecker = serviceProvider.GetService<IPermissionChecker>();

var hasView = await permissionChecker.CheckAsync("Sample.View");
Console.WriteLine($"Sample.View: {hasView}");  // True

var hasDelete = await permissionChecker.CheckAsync("Sample.Delete");
Console.WriteLine($"Sample.Delete: {hasDelete}");  // False

// 检查功能
var featureChecker = serviceProvider.GetService<IFeatureChecker>();

var hasBasic = await featureChecker.IsEnabledAsync("Sample.Feature.Basic");
Console.WriteLine($"Sample.Feature.Basic: {hasBasic}");  // True

var hasAdvanced = await featureChecker.IsEnabledAsync("Sample.Feature.Advanced");
Console.WriteLine($"Sample.Feature.Advanced: {hasAdvanced}");  // False (依赖未满足)
```

## 📦 创建新模块

创建新的模块只需以下几个步骤：

### 1. 创建模块项目

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <ProjectReference Include="..\PluginFramework.Core\PluginFramework.Core.csproj" />
    </ItemGroup>
</Project>
```

### 2. 实现模块类

```csharp
[Module(ModuleStartLevel.Application, Name = "MyModule", Version = "1.0.0")]
public class MyModule : ModuleBase
{
    public override Type[] Dependencies => Array.Empty<Type>();

    public override void ConfigureServices()
    {
        Services.AddTransient<IMyService, MyService>();
    }
}
```

### 3. 配置权限和功能

```csharp
public class MyPermissionProvider : IPermissionProvider
{
    public void Define(IPermissionDefinitionContext context)
    {
        context.GetOrCreatePermission("MyModule.Use", "MyModule", "使用模块");
    }
}

public class MyFeatureProvider : IFeatureProvider
{
    public void Define(IFeatureDefinitionContext context)
    {
        context.GetOrCreateFeature("MyModule.Feature", "MyModule", "模块功能");
    }
}
```

### 4. 注册到应用

```csharp
ApplicationBuilder.Create()
    .AddModule<MyModule>()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IPermissionProvider, MyPermissionProvider>();
        services.AddSingleton<IFeatureProvider, MyFeatureProvider>();
    })
    .Build();
```

## 🎯 最佳实践

1. **模块独立性**：每个模块应该独立完整，不依赖其他模块的内部实现
2. **接口定义**：服务应该通过接口定义，便于替换和测试
3. **依赖管理**：合理声明模块依赖，避免循环依赖
4. **资源释放**：在Shutdown方法中正确释放资源
5. **权限验证**：敏感操作应使用RequiresPermissionAttribute进行标记
6. **配置管理**：使用Configuration获取模块特定配置

## 📚 技术栈

- **.NET 10.0**
- **Microsoft.Extensions.DependencyInjection** - 依赖注入容器
- **Microsoft.Extensions.Hosting.Abstractions** - 主机抽象

## 📄 许可证

本项目采用 MIT 许可证。
