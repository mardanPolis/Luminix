using System;
using System.Reflection;

namespace Luminix;

public abstract class ModuleBehavior
{
    internal ModuleBehavior(Module module)
    {
        this.module = module;
    }

    private readonly Module module;
    public Module GetModule() => module;
    public Assembly GetAssembly() => GetType().Assembly;

    protected virtual void Initialize() {}
    protected virtual void Start() {}
    protected virtual void Shutdown() {}
}