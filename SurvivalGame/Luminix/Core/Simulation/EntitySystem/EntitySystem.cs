using Microsoft.Xna.Framework;
using Luminix.Identifiers;
using System;

namespace Luminix;

public abstract class EntitySystem
{
    public abstract Type[] GetComponentTypes();
}