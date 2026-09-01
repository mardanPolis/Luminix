using System;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class AtlasAsset : Asset
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

    internal AtlasAsset(string identifier, Color[] colors, Vector2 size, Region[] regions) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));
        ArgumentNullException.ThrowIfNull(regions, nameof(regions));

        Colors = colors;
        Size = size;
        Regions = regions;
    }

    public Color[] Colors { get; }
    public Vector2 Size { get; }
    public Region[] Regions { get; }

    public override string ToString()
    {
        return $"Atlas [ Size: {Size}; Regions: {Regions.Length} ]";
    }
}
