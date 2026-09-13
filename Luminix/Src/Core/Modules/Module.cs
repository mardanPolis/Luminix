using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Luminix;

public sealed class Module
{
    [JsonConstructor]
    internal Module()
    {
        supportedVersions = [];
        dependencies = [];
    }

    private string id;

    public required string ID 
    {
        get => id;
        init => id = value.ToLower();
    }
    public required string Name { get; init; }
    public required string Author { get; init; }
    public required Version Version { get; init; }
    public string Description { get; init; }

    [JsonInclude] private Version[] supportedVersions;
    [JsonInclude] private string[] dependencies;

    [JsonIgnore] public ReadOnlySpan<Version> SupportedVersions => supportedVersions;
    [JsonIgnore] public ReadOnlySpan<string> Dependencies => dependencies;

    [JsonInclude] internal string Assembly { get; init; }
    private Assembly loadedAssembly;

    public bool HasAssembly() => !string.IsNullOrWhiteSpace(Assembly);
    public bool IsAssemblyLoaded() => HasAssembly() && loadedAssembly != null;
    public string GetAssemblyName() => Assembly;
    public Assembly GetAssembly() => loadedAssembly;
    internal void SetAssembly(Assembly assembly) => loadedAssembly = assembly;
    

    private string moduleDirectory { get; set; }
    public string GetModuleDirectory() => moduleDirectory;
    internal void SetModuleDirectory(string directory)
    {
        moduleDirectory = directory;
    }

    private ModuleBehavior behavior;
    public bool HasBehavior() => behavior != null;
    public ModuleBehavior GetBehavior() => behavior;
    internal bool TryGetModuleBehavior(Module module, out ModuleBehavior behavior)
    {
        behavior = null;
        if(!module.HasAssembly())
            return false;

        behavior = this.behavior;
        if(module.HasBehavior())
            return true;

        Type[] types = loadedAssembly.GetTypes().Where(t => typeof(ModuleBehavior).IsAssignableFrom(t) && !t.IsAbstract).ToArray();
 
        if (types.Length == 0)
            return false;
 
        Type type = types[0];
 
        if (types.Length > 1)
            for (int i = 1; i < types.Length; i++)
                Logger.NewLog(Logger.Log.Level.Warning, $"Ignoring additional ModuleBehavior type: {types[i].FullName}");
 
        behavior = (ModuleBehavior)Activator.CreateInstance(type, module);
        this.behavior = behavior;
        return true;
    }

    public override bool Equals(object obj)
    {
        return obj is Module moduleAbout && moduleAbout.ID.Equals(this.ID);
    }
    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }
}