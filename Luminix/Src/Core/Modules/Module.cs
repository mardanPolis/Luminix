using System;
using System.Reflection;

namespace Luminix;

public abstract class Module
{
    internal Module(ModuleAbout moduleAbout, Assembly assembly)
    {
        this.moduleAbout = moduleAbout;
        this.assembly = assembly;
    }

    private readonly ModuleAbout moduleAbout;
    private readonly Assembly assembly;
    public ModuleAbout GetAbout() => moduleAbout;
    public Assembly GetAssembly() => assembly;

    protected virtual void Initialize() {}
    protected virtual void Shutdown() {}
}