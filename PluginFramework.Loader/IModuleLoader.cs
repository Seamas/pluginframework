using PluginFramework.Loader.Metadata;

namespace PluginFramework.Loader;

/// <summary>
/// 模块加载器：负责从指定目录加载模块 Assembly，扫描元数据，并过滤权限
/// </summary>
public interface IModuleLoader
{
    /// <summary>
    /// 从指定目录加载所有模块，按角色过滤
    /// </summary>
    /// <param name="modulesDirectory">模块 DLL 所在目录</param>
    /// <param name="userRoles">当前用户的角色列表</param>
    /// <returns>用户有权访问的模块元数据列表</returns>
    Task<IReadOnlyList<ModuleMeta>> LoadModulesAsync(string modulesDirectory, string[] userRoles);
}