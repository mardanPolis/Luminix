using Luminix.Collections;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class TextureResource : Resource
{
    private static uint globalIndex = 0;
    internal TextureResource(string name, Rectangle rectangle, DenseList<TextureResource> subTextures = null) 
        : base(name)
    {
        Rectangle = rectangle;
        SubTextures = subTextures;
        Index = globalIndex;
        globalIndex++;
    }

    internal uint Index { get; }

    internal TextureResource(string name, int posX, int posY, int width, int height, DenseList<TextureResource> subTextures = null) 
        : this(name, new Rectangle(posX, posY, width, height), subTextures) { }

    internal DenseList<TextureResource> SubTextures { get; }

    public Rectangle Rectangle { get; }

    public bool IsSource => SubTextures != null;

    public int Width => Rectangle.Width;
    public int Height => Rectangle.Height;

    public sealed override string ToString() => $"{Identifier} {Rectangle}";
}
