using System;
using Microsoft.Xna.Framework;

namespace SurvivalGame.Engine.EntitySystem;

public abstract class StaticEntity : Entity
{
    protected StaticEntity(uint id, TypeValues typeValues) : base(id, typeValues)
    { 
    }

    public struct Instance
    {
        public Vector2 Position { get; set;}
        public float Rotation { get; set;}
        public Vector2 Scale { get; set;}
    }
}
