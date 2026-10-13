namespace PluginFramework.Loader;

/// <summary>
/// 权限 key 工具类。
/// 
/// Key 格式规定：
///   Operation 权限: "ModuleCode:FeatureCode:OperationCode"
///   Feature 通配符:  "ModuleCode:FeatureCode:*"
/// 
/// 级联规则：用户拥有任何 Operation 权限，自动获得其所属 Feature 和 Module 的可见性。
/// </summary>
public static class PermissionKeyHelper
{
    /// <summary>key 分隔符</summary>
    public const char Separator = ':';

    /// <summary>通配符</summary>
    public const string Wildcard = "*";

    /// <summary>构建 Operation 权限 key</summary>
    public static string BuildOperationKey(string moduleCode, string featureCode, string operationCode)
        => $"{moduleCode}{Separator}{featureCode}{Separator}{operationCode}";

    /// <summary>构建 Feature 级通配 key（匹配该 Feature 下所有 Operation）</summary>
    public static string BuildFeatureWildcardKey(string moduleCode, string featureCode)
        => $"{moduleCode}{Separator}{featureCode}{Separator}{Wildcard}";

    /// <summary>
    /// 检查用户是否有权限访问某个 Operation。
    /// 匹配规则：用户的 permissionKeys 中包含：
    ///   1. 精确匹配 "ModuleCode:FeatureCode:OperationCode"
    ///   2. 通配匹配 "ModuleCode:FeatureCode:*"
    /// </summary>
    public static bool HasOperationPermission(
        HashSet<string> userPermissionKeys,
        string moduleCode,
        string featureCode,
        string operationCode)
    {
        var exactKey = BuildOperationKey(moduleCode, featureCode, operationCode);
        if (userPermissionKeys.Contains(exactKey))
            return true;

        var wildcardKey = BuildFeatureWildcardKey(moduleCode, featureCode);
        return userPermissionKeys.Contains(wildcardKey);
    }

    /// <summary>
    /// 检查用户是否有权限访问某个 Feature 下的任何 Operation
    /// </summary>
    public static bool HasFeaturePermission(
        HashSet<string> userPermissionKeys,
        string moduleCode,
        string featureCode)
    {
        var prefix = $"{moduleCode}{Separator}{featureCode}{Separator}";
        return userPermissionKeys.Any(k => k.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <summary>
    /// 检查用户是否有权限访问某个 Module 下的任何 Feature/Operation
    /// </summary>
    public static bool HasModulePermission(
        HashSet<string> userPermissionKeys,
        string moduleCode)
    {
        var prefix = $"{moduleCode}{Separator}";
        return userPermissionKeys.Any(k => k.StartsWith(prefix, StringComparison.Ordinal));
    }
}