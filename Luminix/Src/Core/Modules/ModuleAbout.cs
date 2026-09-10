using System;
using System.Reflection;

namespace Luminix;

public sealed class ModuleAbout
{
    internal ModuleAbout() {}

    public required string ID { get; init; }
    public string Name { get; init; }
    public string Author { get; init; }
    public Version Version { get; init; }
    public string Description { get; init; }

    public string Assembly { get; init; }
    public string ModulePath { get; init; }

    private Version[] supportedVersions;
    private string[] dependencies;

    public ReadOnlySpan<Version> SupportedVersions => supportedVersions;
    public ReadOnlySpan<string> Dependencies => dependencies;

    private Module module;

    public Module GetModule() => module;

    internal void SetModule(Module module)
    {
        this.module = module;
    }
}