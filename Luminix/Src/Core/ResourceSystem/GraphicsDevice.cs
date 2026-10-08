using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.IO;
using System.Linq;

namespace Luminix;

internal sealed class GraphicResourceManager : Microsoft.Xna.Framework.Content.ContentManager
{
    internal GraphicResourceManager(MainRender main) : base(main.Services)
    {
        this.main = main;

        this.sourceResources = [];
        this.linked = [];

        Color[] nullTexturePixels = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                nullTexturePixels[y * 32 + x] = (x / 16 ^ y / 16) == 1
                    ? Color.Magenta
                    : Color.Black;

        NullTexture = new Texture2D(main.GraphicsDevice, 32, 32);
        NullTexture.SetData(nullTexturePixels);
    }

    public new string RootDirectory => AssetIO.ASSET_DIRECTORY;

    private MainRender main;
    private bool disposed = false;

    private Stream openedStream = null; 

    private Texture2D NullTexture { get; }

    private readonly Dictionary<(GraphicResourceType, string), object>  sourceResources;
    private readonly Dictionary<Resource, object> linked;

    public void CreateTexture(string identifier, Color[] colors, int X, int Y)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var source = new Texture2D(main.GraphicsDevice, X, Y);
        source.SetData(colors);

        sourceResources[(GraphicResourceType.Texture, identifier)] = source;
    }
    public void CreateFont(string identifier, byte[] data)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var stream = new MemoryStream(data);
        var source = Create<SpriteFont>(identifier, stream);
        sourceResources[(GraphicResourceType.Font, identifier)] = source;
    }
    public void CreateShader(string identifier, byte[] data)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var stream = new MemoryStream(data);
        var source = Create<Effect>(identifier, stream);
        sourceResources[(GraphicResourceType.Shader, identifier)] = source;
    }

    public void UpdateTexture(string identifier, Color[] colors)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if(!sourceResources.TryGetValue((GraphicResourceType.Texture, identifier), out object source))
            throw new KeyNotFoundException($"Failed to find source texture with '{identifier}' identifier.");

        ((Texture2D)source).SetData(colors);
    }
    public void Unload(GraphicResourceType resourceType, string identifier)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if(!sourceResources.TryGetValue((resourceType, identifier), out object source))
            return;
        
        var staleLinks = linked.Where(p => ReferenceEquals(p.Value, source))
                            .Select(p => p.Key)
                            .ToList();
        foreach (var resource in staleLinks)
            linked.Remove(resource);
            
        if(source is IDisposable disposable)
            disposable.Dispose();
        
        sourceResources.Remove((resourceType, identifier));
    }
    public sealed override void Unload()
    {
        foreach(var key in sourceResources.Keys)
        {
            Unload(key.Item1, key.Item2);
        }
    }

    public void LinkTexture(string identifier, TextureResource resource)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if(!sourceResources.TryGetValue((GraphicResourceType.Texture, identifier), out object source))
            throw new KeyNotFoundException($"Failed to find source texture with '{identifier}' identifier.");

        if(linked.TryGetValue(resource, out object otherSource))
        {
            if(otherSource == source)
                return;

            throw new InvalidOperationException($"Failed to link resource '{resource}' with an source, resource is already linked with another source.");
        }

        linked[resource] = source;
    }
    public void LinkFont(string identifier, FontResource resource)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if(!sourceResources.TryGetValue((GraphicResourceType.Font, identifier), out object source))
            throw new KeyNotFoundException($"Failed to find source font with '{identifier}' identifier.");

        if(linked.TryGetValue(resource, out object otherSource))
        {
            if(otherSource == source)
                return;
                
            throw new InvalidOperationException($"Failed to link resource '{resource}' with an source, resource is already linked with another source.");
        }

        linked[resource] = source;
    }
    public void LinkShader(string identifier, ShaderResource resource)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if(!sourceResources.TryGetValue((GraphicResourceType.Shader, identifier), out object source))
            throw new KeyNotFoundException($"Failed to find source shader with '{identifier}' identifier.");

        if(linked.TryGetValue(resource, out object otherSource))
        {
            if(otherSource == source)
                return;
                
            throw new InvalidOperationException($"Failed to link resource '{resource}' with an source, resource is already linked with another source.");
        }

        linked[resource] = source;
    }

    public void Unlink(Resource resource)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!linked.ContainsKey(resource))
            return;

        linked.Remove(resource);
    }

    private T Create<T>(string identifier, Stream stream) where T : class
    {
        T result = null;

        try
        {
            openedStream = stream;
            result = ReadAsset<T>(identifier, null);
        }
        finally
        {
            openedStream = null;
        }

        return result;
    }
    protected sealed override Stream OpenStream(string relativePath)
    {
        if(openedStream != null)
            return openedStream;

        return base.OpenStream(relativePath);
    }
    
    [Obsolete("Use Unload(Resource) instead")]
    public sealed override void UnloadAsset(string assetName)
    {
        throw new NotSupportedException(nameof(UnloadAsset));
    }

    [Obsolete("Use Unload() or Unload(Resource) instead")]
    public sealed override void UnloadAssets(IList<string> assetNames)
    {
        throw new NotSupportedException(nameof(UnloadAssets));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        disposed = true;
    }
}