using System;
using System.Reflection;
using SurvivalGame.Core;

namespace SurvivalGame.Modding;

public class Mod
{
    public readonly string Name;
    public readonly string ModId;
    public readonly string Description;
    public readonly string Version;
    public readonly string[] TarVersion;
    public readonly string[] Dependencies;
    public readonly string[] LoadAfter;
    public readonly string[] LoadBefore;

    public readonly string Location;
    public const string PICTURE = "icon";

    public Assembly Assembly { get; internal set; }

    public Mod(string name, string modId, string version, string[] tarVersions, string location, string desc = null, string[] dependencies = null, string[] loadAfter = null, string[] loadBefore = null)
    {
        Name = name;
        ModId = modId;
        Description = desc ?? "Mod description";
        Version = version;
        TarVersion = tarVersions;
        Dependencies = dependencies ?? [];
        LoadAfter = loadAfter ??  [];
        LoadBefore = loadBefore ?? [];
        Location = location;
    }

    public virtual void RegisterTags() {}
    public virtual void RegisterEntities() {}
    public virtual void RegisterEntityRenderers() {}

    public virtual void OnLoad() {}
    public virtual void OnUnload() {}

    public virtual void OnLoadContent() {}
    public virtual void OnUnloadContent() {}

    public virtual void Start() {}
    public virtual void Stop() {}

    public virtual void PreGameTick() {}
    public virtual void GameTick() {}
    public virtual void PostGameTick() {}
}