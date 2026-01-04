using Microsoft.Xna.Framework;

namespace SurvivalGame.Core.EntitySystem;

public abstract class DynamicEntity : Entity
{
    public Vector2 Position { set; get; }
    public float Rotation { set; get; }
    public Vector2 Scale { set; get; }

    public abstract void PreTick();
    public abstract void Tick();
    public abstract void PostTick();
}
