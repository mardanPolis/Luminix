using System;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix;


public sealed class FontResource : Resource
{
    internal FontResource(string name) : base(name)
    {
    }

    public sealed override string ToString() => Identifier;
}
