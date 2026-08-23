using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix;

public sealed class Resources : Workspace.Service, IEnumerable<Resource>
{
    internal static class BuiltIn
    {
        public static Color[] NullTextureColor
        {
            get
            {
                Color[] pixels = new Color[32 * 32];
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                        pixels[y * 32 + x] = ((x / 16 ^ y / 16) & 1) == 1
                            ? Color.Magenta
                            : Color.Black;
                return pixels;
            }
        }

        public static Color[] PixelTextureColor
        {
            get
            {
                Color[] pixels = new Color[1];
                pixels[0] = Color.White;
                return pixels;
            }
        }
    }
    internal Resources() { }
 
    public int ResourceCount => resources != null ? resources.Count : 0;
 
    private FrozenDictionary<(Type, string), Resource> resources;

    public T GetResource<T>(string identifier) where T : Resource
    {
        identifier = identifier.ToLower();
        Type type = typeof(T);

        if (resources != null && resources.TryGetValue((type, identifier), out Resource resource))
            return (T)resource;

        Logger.Get().Wrn($"Resource identified '{identifier}' was not found.");
        return null;
    }

    internal void Set(
    IReadOnlyDictionary<string, TextureResource> textures,
    IReadOnlyDictionary<string, AudioResource> audios,
    IReadOnlyDictionary<string, FontResource> fonts,
    IReadOnlyDictionary<string, ShaderResource> shaders)
    {
        var combined = new Dictionary<(Type, string), Resource>(
            textures.Count + audios.Count + fonts.Count + shaders.Count);

        var textureType = typeof(TextureResource);
        var audioType = typeof(TextureResource);
        var fontType = typeof(TextureResource);
        var shaderType = typeof(TextureResource);

        foreach (var (key, value) in textures)
            combined[(textureType, key)] = value;

        foreach (var (key, value) in audios)
            combined[(audioType, key)] = value;

        foreach (var (key, value) in fonts)
            combined[(fontType, key)] = value;

        foreach (var (key, value) in shaders)
            combined[(shaderType, key)] = value;

        resources = combined.ToFrozenDictionary();
    }

    internal void Set(FrozenDictionary<(Type, string), Resource> resources)
    {
        this.resources = resources;
    }

    internal FrozenDictionary<(Type, string), Resource> GetResources() => resources;

    public IEnumerator<Resource> GetEnumerator()
    {
        if (resources == null)
            yield break;

        foreach (var resource in resources.Values)
            yield return resource;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}


public abstract class Resource
{
    public readonly string Name;
    internal Resource(string name)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Name = name;
    }
}

public sealed class TextureResource : Resource
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

public sealed class AudioResource : Resource
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