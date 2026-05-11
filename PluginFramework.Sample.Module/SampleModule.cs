using Microsoft.Extensions.DependencyInjection;
using PluginFramework.Core;
using PluginFramework.Core.Attributes;

namespace PluginFramework.Sample.Module;

[ModuleMeta("sample")]
public class SampleModule: IModuleConfigure
{
    public string Name { get; } = "我的测试功能";
    
    public Type UIType { get; } = typeof(MyCustomControl);


    public void ConfigureServices(IServiceCollection services)
    {
        // throw new NotImplementedException();
    }
}