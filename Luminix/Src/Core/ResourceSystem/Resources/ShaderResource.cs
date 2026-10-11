using System;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix;

public sealed class ShaderResource : Resource
{
    internal ShaderResource(ShaderID source, string identifier)
        : base(identifier)
    {
        Source = source;
    }

    internal ShaderID Source { get; }

    public sealed override string ToString() => Identifier;
}
