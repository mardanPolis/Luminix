using System;
using SurvivalGame.Engine.EntitySystem;

namespace SurvivalGame.Core;

public static class GameRegistry
{
    const string INVINCIBLE = "invincible";
    const string UNBREAKABLE = "unbreakable";

    public static Entity.Tag TagInvincible { private set; get; }
    public static Entity.Tag TagUnbreakable { private set; get; }

    internal static void SetUp()
    {
        EntityTagRegistry.TryGet(INVINCIBLE, out Entity.Tag TagInvincible);
        GameRegistry.TagInvincible = TagInvincible;
        EntityTagRegistry.TryGet(UNBREAKABLE, out Entity.Tag TagUnbreakable);
        GameRegistry.TagUnbreakable = TagUnbreakable;
    }

    internal static void RegisterTags()
    {
        EntityTagRegistry.RegisterTag(INVINCIBLE);
        EntityTagRegistry.RegisterTag(UNBREAKABLE);
    }
    internal static void RegisterEntities()
    {
        EntityTypeRegistry.Register($"some_entity", EntityTypeRegistry.NewBuilder<EntityTypeBuilder, SomeEntity>().Tagged(INVINCIBLE, false));
    }
    internal static void RegisterRenderers()
    {
        
    }
}
