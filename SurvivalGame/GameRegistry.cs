using System;
using SurvivalGame.Core.EntitySystem;

namespace SurvivalGame;

public static class GameRegistry
{
    internal static void RegisterTags()
    {
        Entities.RegisterTag("invincible");
    }
    internal static void RegisterEntities()
    {
        Entities.Register($"{SurvivalGame.GameId}:some_entity", Entities.NewBuilder<EntityBuilder, SomeEntity>().Tagged("invincible", false));
    }
    internal static void RegisterRenderers()
    {
        
    }
}
