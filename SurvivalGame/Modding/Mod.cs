using System;
using System.Reflection;
using SurvivalGame.Core;

namespace SurvivalGame.Modding;

public class Mod
{
    public readonly string ModId;
    public Assembly Assembly { get; internal set; }

    public Mod(string id)
    {
        ModId = id;
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

    public ModInfo GetModInfo()
    {
        if(ModHandler.TryGetModInfo(ModId, out ModInfo info))
            return info;
        
        throw new Exception("Something went wrong!");
    }
}