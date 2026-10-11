using System;
using System.Reflection;

namespace Luminix;

public abstract class ModuleBehavior
{
    public ModuleBehavior()
    {
        Workspace = Workspace.Current;
    }

    protected readonly Workspace Workspace;

    internal Module Module;
    public Module GetModule() => Module;
    public Assembly GetAssembly() => GetType().Assembly;

    protected virtual void Initialize() {}
    protected virtual void Start() {}
    protected virtual void Shutdown() {}
}