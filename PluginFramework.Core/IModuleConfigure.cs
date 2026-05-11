using Microsoft.Extensions.DependencyInjection;

namespace PluginFramework.Core;

public interface IModuleConfigure
{
    
    string Name { get; }

    Type UIType { get; }
    
    void ConfigureServices(IServiceCollection services);
}