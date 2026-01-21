using System;
using System.Xml.Serialization;
using SurvivalGame.Core;

namespace SurvivalGame.Modding;

public abstract class Mod
{
    public ModInfo Info { get; internal set; }

    public abstract void RegisterTags();
    public abstract void RegisterEntities();
    public virtual void RegisterEntityRenderers() {}

    public virtual void OnLoad() {}
    public virtual void OnUnload() {}

    public virtual void GameTick() {}
}