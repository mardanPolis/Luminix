using System;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix;

public sealed class ShaderResource : Resource
{
    private static uint globalIndex = 0;
    internal ShaderResource(string identifier)
        : base(identifier)
    {
        Index = globalIndex;
        globalIndex++;
    }

    internal uint Index { get; }

    public sealed override string ToString() => Identifier;
}
