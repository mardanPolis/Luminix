using System;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix.Resources;

public abstract class Resource
{
    public readonly string Name;
    internal Resource(string name)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Name = name;
    }
}

public class TextureResource : Resource
{
    internal TextureResource(Texture2D source, string name, Rectangle rectangle) 
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));

        Source = source;
        Rectangle = rectangle;
    }

    internal TextureResource(Texture2D source, string name, int posX, int posY, int width, int height) 
        : this(source, name, new Rectangle(posX, posY, width, height)) { }
        
    internal readonly Texture2D Source;
    
    public readonly Rectangle Rectangle;

    public int Width => Rectangle.Width;
    public int Height => Rectangle.Height;

    public int SourceWidth  => Source.Width;
    public int SourceHeight => Source.Height;

    public bool IsDisposed => Source.IsDisposed;

    public sealed override string ToString() => $"{Name} {Rectangle}";
}

public sealed class FontResource : Resource
{
    internal FontResource(SpriteFont source, string name)
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));

        Source = source;
    }

    internal readonly SpriteFont Source;

    public ReadOnlyCollection<char> Characters => Source.Characters;

    public bool IsDisposed => Source.Texture.IsDisposed;

    public sealed override string ToString() => Name;
}

public sealed class ShaderResource : Resource
{
    internal ShaderResource(Effect source, string name)
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));

        Source = source;
    }

    internal readonly Effect Source;

    public bool IsDisposed => Source.IsDisposed;

    public sealed override string ToString() => Source.ToString();
}

public class AudioResource : Resource
{
    internal AudioResource(SoundEffect source, string name) 
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));

        Source = source;
    }
    internal readonly SoundEffect Source;

    public TimeSpan Duration => Source.Duration;
    public bool IsDisposed => Source.IsDisposed;

    public void Play() => Source.Play();
    public SoundEffectInstance CreateInstance() => Source.CreateInstance();
    
    public sealed override string ToString() => Source.ToString();
}