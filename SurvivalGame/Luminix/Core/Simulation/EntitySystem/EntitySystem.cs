using Microsoft.Xna.Framework;
using Luminix.Identifiers;
using System;

namespace Luminix;

public abstract class EntitySystem
{
    internal EntitySystem(string entityTypeName, Entity.TypeID entityTypeID)
    {
        this.entityTypeName = entityTypeName;
        this.entityTypeID = entityTypeID;
    }
    protected string entityTypeName;
    protected Entity.TypeID entityTypeID;
    public abstract Type[] GetComponentTypes();
}