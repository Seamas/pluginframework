using Microsoft.Extensions.DependencyInjection;
using PluginFramework.Core;
using PluginFramework.Core.Attributes;

namespace PluginFramework.Sample.Module;

/// <summary>
/// 示例模块：通过 [ModuleMeta] 标记，自动被扫描器发现
/// 实现 IModuleConfigure 以注册模块内部所需的 DI 服务
/// </summary>
[ModuleMeta("sample", Name = "示例模块")]
public class SampleModule : IModuleConfigure
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册 ViewModel（供 DI 容器创建实例）
        services.AddTransient<MyCustomControlViewModel>();

        // 注册此模块内部需要的其他服务
        // services.AddSingleton<ISomeService, SomeServiceImpl>();
    }
}