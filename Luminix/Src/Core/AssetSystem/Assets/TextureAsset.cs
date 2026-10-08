using System;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class TextureAsset : Asset
{
    public TextureAsset(string identifier, Color[] colors, Vector2 size) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));

        Colors = colors;
        Size = size;
    }

    public Color[] Colors { get; private set; }
    public Vector2 Size { get; private set; }

    protected override void OnDispose()
    {
        Colors = null;
    }
}
