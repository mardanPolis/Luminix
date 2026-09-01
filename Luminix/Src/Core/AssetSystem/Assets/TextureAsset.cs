using System;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class TextureAsset : Asset
{
    internal TextureAsset(string identifier, Color[] colors, Vector2 size) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));

        Colors = colors;
        Size = size;
    }

    public Color[] Colors { get; }
    public Vector2 Size { get; }
}
