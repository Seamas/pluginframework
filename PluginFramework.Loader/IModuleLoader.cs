namespace PluginFramework.Loader;

public interface IModuleLoader
{
    Task LoadModulesAsync(string moduleName);
}