using System;
using System.Reflection;

namespace Luminix;

public abstract class Module
{
    public string ID;
    public string Name => GetType().Name;
    public string Author;
    public virtual Version Version { get; } = new(1, 0, 0);
    public string Description;

    public string[] supportedVersions;

    public string[] dependencies;

    public Assembly Assembly;
    public string ModulePath;

    

    protected virtual void Initialize() {}
    protected virtual void PostInitialize() {}
    protected virtual void Shutdown() {}
}