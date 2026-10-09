using System;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix;


public sealed class FontResource : Resource
{
    private static uint globalIndex = 0;
    internal FontResource(string name) : base(name)
    {
        Index = globalIndex;
        globalIndex++;
    }

    internal uint Index { get; }
    public sealed override string ToString() => Identifier;
}
