using System;
using Microsoft.Xna.Framework;

namespace Luminix;

internal sealed class AtlasAsset : Asset
{
    public readonly record struct Region
    {
        internal Region(string identifier, Rectangle rectangle)
        {
            Identifier = identifier.ToLower();
            Rectangle = rectangle;
        }

        public string Identifier { get; }
        public Rectangle Rectangle { get; }

        public override string ToString()
        {
            return $"Region [ Name: {Identifier}; Rectangle: {Rectangle} ]";
        }
    };

    internal AtlasAsset(string identifier, Color[] colors, Point size, Region[] regions) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));
        ArgumentNullException.ThrowIfNull(regions, nameof(regions));

        Colors = colors;
        Size = size;
        Regions = regions;
    }

    public Color[] Colors { get; private set; }
    public Point Size { get; }
    public Region[] Regions { get; private set; }

    public override string ToString()
    {
        return $"Atlas [ Size: {Size}; Regions: {Regions.Length} ]";
    }

    protected override void OnDispose()
    {
        Colors = null;
        Regions = null;
    }
}
