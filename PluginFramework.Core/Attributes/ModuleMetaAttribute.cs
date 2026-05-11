namespace PluginFramework.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ModuleMetaAttribute(string code) : Attribute
{
    public string Code { get; } = code;
    
}