using System;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix;

public sealed class ShaderResource : Resource
{
    internal ShaderResource(string identifier)
        : base(identifier)
    {
    }

    public sealed override string ToString() => Identifier;
}
