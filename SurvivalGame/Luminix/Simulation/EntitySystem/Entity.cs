using System;
using Microsoft.Xna.Framework;
using Luminix.Identifiers;

namespace Luminix.EntitySystem;

public abstract class Entity : IIdentifiable, IVariant
{
    public Entity(ID iD, Variant variant)
    {
        this.id = iD;
        this.variant = variant;
    }

    private readonly ID id;
    private readonly Variant variant;
    private bool isDestroyed = false;
    
    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Rotation { get; set; } = 0;
    public Vector2 Scale { get; set; } = Vector2.One;

    public virtual void Awake() {}
    public virtual void Start() {}
    public virtual void Update() {}

    public virtual void OnDestroy() {}

    public bool IsDestroyed() => isDestroyed;

    public static void Destroy(Entity entity)
    {
        entity.OnDestroy(); 
        entity.isDestroyed = true;
    }

    public ID GetID() => id;
    public Variant GetVariant() => variant;
}
