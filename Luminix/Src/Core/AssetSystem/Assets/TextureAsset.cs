using System;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class TextureAsset : Asset
{
    public TextureAsset(string identifier, Color[] colors, Point size) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));

        Colors = colors;
        Size = size;
    }

    public Color[] Colors { get; private set; }
    public Point Size { get; private set; }

    protected override void OnDispose()
    {
        Colors = null;
    }
}
