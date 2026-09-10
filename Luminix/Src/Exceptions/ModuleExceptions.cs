using System;

namespace Luminix.Exceptions;

public abstract class ModuleException : Exception
{
    public string ModuleID { get; }
    protected ModuleException(string moduleId, string message) : base(message)
        => ModuleID = moduleId;
}

public sealed class DuplicateModuleException : ModuleException
{
    public string ModuleDirectory { get; }
    public DuplicateModuleException(string moduleId, string moduleDirectory)
        : base(moduleId, $"A module with ID '{moduleId}' is already registered (from '{moduleDirectory}').")
        => ModuleDirectory = moduleDirectory;
}

public sealed class ModuleNotFoundException : ModuleException
{
    public ModuleNotFoundException(string moduleId)
        : base(moduleId, $"No module with ID '{moduleId}' is registered.") { }
}

public sealed class ModuleNotInitializedException : ModuleException
{
    public ModuleNotInitializedException(string moduleId)
        : base(moduleId, $"Module '{moduleId}' is registered but has not been initialized.") { }
}

public sealed class InvalidModuleManifestException : ModuleException
{
    public string ModuleDirectory { get; }
    public InvalidModuleManifestException(string moduleDirectory, string reason, Exception inner = null)
        : base(moduleId: "(unknown)", $"Invalid module manifest in '{moduleDirectory}': {reason}")
        => ModuleDirectory = moduleDirectory;
}

public sealed class ModuleInitializationException : ModuleException
{
    public ModuleInitializationException(string moduleId, Exception inner)
        : base(moduleId, $"Module '{moduleId}' threw during initialization: {inner.Message}")
    {
    }
}

public sealed class ModuleDependencyException : ModuleException
{
    public string MissingDependencyID { get; }
    public ModuleDependencyException(string moduleId, string missingDependencyId)
        : base(moduleId, $"Module '{moduleId}' depends on '{missingDependencyId}', which is not available.")
        => MissingDependencyID = missingDependencyId;
}

public sealed class CircularModuleDependencyException : ModuleException
{
    public CircularModuleDependencyException(string moduleId, string cyclePath)
        : base(moduleId, $"Circular dependency detected involving module '{moduleId}': {cyclePath}") { }
}