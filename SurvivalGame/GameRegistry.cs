using System;
using SurvivalGame.Core.EntitySystem;

namespace SurvivalGame;

public static class GameRegistry
{
    const string invincible = "invincible";
    const string unbreakable = "unbreakable";

    public static Entity.Tag TagInvincible { private set; get; }
    public static Entity.Tag TagUnbreakable { private set; get; }

    internal static void SetUp()
    {
        EntityTagRegistry.TryGet(invincible, out Entity.Tag TagInvincible);
        GameRegistry.TagInvincible = TagInvincible;
        EntityTagRegistry.TryGet(unbreakable, out Entity.Tag TagUnbreakable);
        GameRegistry.TagUnbreakable = TagUnbreakable;
    }

    internal static void RegisterTags()
    {
        EntityTagRegistry.RegisterTag(invincible);
        EntityTagRegistry.RegisterTag(unbreakable);
    }
    internal static void RegisterEntities()
    {
        EntityTypeRegistry.Register($"{SurvivalGame.GameId}:some_entity", EntityTypeRegistry.NewBuilder<EntityTypeBuilder, SomeEntity>().Tagged(invincible, false));
    }
    internal static void RegisterRenderers()
    {
        
    }
}
