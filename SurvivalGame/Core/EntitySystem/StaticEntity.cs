using System;
using Microsoft.Xna.Framework;

namespace SurvivalGame.Core.EntitySystem;

public abstract class StaticEntity : Entity
{
    protected StaticEntity(uint id, TypeValues typeValues) : base(id, typeValues)
    {
    }
}
