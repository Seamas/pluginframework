using PluginFramework.Loader.Metadata;

namespace PluginFramework.Loader;

/// <summary>
/// 模块加载器：负责从指定目录加载模块 Assembly，扫描元数据，并根据用户权限 key 过滤
/// </summary>
public interface IModuleLoader
{
    /// <summary>
    /// 从指定目录加载所有模块，按用户权限 key 过滤
    /// </summary>
    /// <param name="modulesDirectory">模块 DLL 所在目录</param>
    /// <param name="userPermissionKeys">用户拥有的权限 key 集合（如 "Module1:Feature1:Op1"）</param>
    /// <returns>用户有权访问的模块元数据列表（已过滤 Feature 和 Operation）</returns>
    Task<IReadOnlyList<ModuleMeta>> LoadModulesAsync(string modulesDirectory, HashSet<string> userPermissionKeys);
}