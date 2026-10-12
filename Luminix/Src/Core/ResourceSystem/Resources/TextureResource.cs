using Luminix.Collections;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class TextureResource : Resource
{
    internal TextureResource(TextureID source, string name, Rectangle rectangle, DenseList<TextureResource> subTextures = null) 
        : base(name)
    {
        Rectangle = rectangle;
        SubTextures = subTextures;
        Source = source;
    }

    internal TextureID Source { get; }

    internal TextureResource(TextureID source, string name, int posX, int posY, int width, int height, DenseList<TextureResource> subTextures = null) 
        : this(source, name, new Rectangle(posX, posY, width, height), subTextures) { }

    internal DenseList<TextureResource> SubTextures { get; }

    public Rectangle Rectangle { get; }

    public bool IsSource => SubTextures != null;

    public int Width => Rectangle.Width;
    public int Height => Rectangle.Height;

    public sealed override string ToString() => $"[TextureResource: '{Identifier}'; {Rectangle}]";
}
