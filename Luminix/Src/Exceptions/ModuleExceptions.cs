using System;

namespace Luminix.Exceptions;

public abstract class ModuleException : Exception
{
    public string ModuleID { get; }
    protected ModuleException(string moduleId, string message) : base(message)
        => ModuleID = moduleId;

    protected ModuleException(string moduleId, string message, Exception inner) : base(message, inner)
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
        : base(moduleId: "(unknown)", $"Invalid module manifest in '{moduleDirectory}': {reason}", inner)
        => ModuleDirectory = moduleDirectory;
}

public sealed class ModuleDependencyException : ModuleException
{
    public string MissingDependencyID { get; }
    public ModuleDependencyException(string moduleId, string missingDependencyId)
        : base(moduleId, $"Module '{moduleId}' depends on '{missingDependencyId}', which is not available.")
        => MissingDependencyID = missingDependencyId;
}

public sealed class ModuleInitializationException : ModuleException
{
    public ModuleInitializationException(string moduleId, Exception inner)
        : base(moduleId, $"Module '{moduleId}' threw during initialization: {inner.Message}", inner)
    {
    }
}

public sealed class ModuleStartException : ModuleException
{
    public ModuleStartException(string moduleId, Exception inner)
        : base(moduleId, $"Module '{moduleId}' threw while starting: {inner.Message}", inner)
    {
    }
}

public sealed class ModuleShutdownException : ModuleException
{
    public ModuleShutdownException(string moduleId, Exception inner)
        : base(moduleId, $"Module '{moduleId}' threw during shutdown: {inner.Message}", inner)
    {
    }
}
