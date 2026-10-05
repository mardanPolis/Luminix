using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Luminix.Threading.Bridges;
using Luminix.Threading.Commands;
using Microsoft.Xna.Framework;
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
    internal Resources() { this.resourceBridge = ThreadBridges.GetBridge<ResourceBridge>(Application.RENDER_THREAD_NAME); }
 
    public int ResourceCount => resources != null ? resources.Count : 0;
 
    private FrozenDictionary<(Type, string), Resource> resources;

    private ResourceBridge resourceBridge;

    public T GetResource<T>(string identifier) where T : Resource
    {
        identifier = identifier.ToLower();
        Type type = typeof(T);

        if (resources != null && resources.TryGetValue((type, identifier), out Resource resource))
            return (T)resource;

        Logger.NewLog(Logger.Log.Level.Warning, $"Resource identified '{identifier}' was not found.");
        return null;
    }

    internal void Reset()
    {
        this.resources = resourceBridge.GetResourceContainer();
    }

    internal void TryReset()
    {
        if(resourceBridge.ChangedResources)
            this.resources = resourceBridge.GetResourceContainer();
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

    public Task CreateResourceFromAsset(Asset asset)
    {
        var command = new ResourceCommand();
        command.Asset = asset;
        command.Identifier = asset.Identifier;
        command.Kind = ResourceCommand.Type.create;
        return resourceBridge.NewCommand(command);
    }

    public Task UnloadResource(Resource resource)
    {
        var command = new ResourceCommand();
        command.Identifier = resource.Identifier;
        command.Kind = ResourceCommand.Type.unload;
        return resourceBridge.NewCommand(command);
    }
}


public abstract class Resource
{
    public readonly string Identifier;
    internal Resource(string name)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Identifier = name;
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

    public sealed override string ToString() => $"{Identifier} {Rectangle}";
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

    public sealed override string ToString() => Identifier;
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
    internal AudioResource(AudioClip clip, string name) 
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(clip, nameof(clip));

        this.clip = clip;
    }

    private AudioClip clip;
    
    public AudioClip Clip => clip;
    public bool IsDisposed => clip.IsDisposed;

    public sealed override string ToString()
    {
        string identifier = !string.IsNullOrEmpty(Identifier) ? Identifier : "Unset";
        return $"[AudioResource: '{identifier}' | {clip}]";
    }
}