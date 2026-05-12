using Microsoft.Extensions.DependencyInjection;

namespace PluginFramework.Core;

public interface IModuleConfigure
{
    
    void ConfigureServices(IServiceCollection services);
}