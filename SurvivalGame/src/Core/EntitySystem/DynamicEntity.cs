using Microsoft.Xna.Framework;

namespace SurvivalGame.Core.EntitySystem;

public abstract class DynamicEntity : Entity
{
    protected DynamicEntity(uint id, TypeValues typeValues) : base(id, typeValues) { }

    public Vector2 Position { set; get; }
    public float Rotation { set; get; } = 0;
    public Vector2 Scale { set; get; } = Vector2.One;

    public abstract void PreTick();
    public abstract void Tick();
    public abstract void PostTick();

    public override string ToString()
    {
        return $"Entity -> class: {this.GetType().Name}, id: {Id}, position: {Position}, rotation: {Rotation}, scale: {Scale}";
    }
}
