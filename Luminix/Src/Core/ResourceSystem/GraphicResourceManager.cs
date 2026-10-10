using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.IO;
using System.Linq;

namespace Luminix;

public sealed class GraphicsResourceManager : Microsoft.Xna.Framework.Content.ContentManager
{
    internal GraphicsResourceManager(MainRender main) : base(main.Services)
    {
        this.main = main;

        this.sourceResources = [];
        this.linked = [];
    }

    public new string RootDirectory => AssetIO.ASSET_DIRECTORY;

    private MainRender main;
    private bool disposed = false;

    private Stream openedStream = null; 

    private readonly Dictionary<(GraphicResourceType, string), object>  sourceResources;
    private readonly Dictionary<Resource, object> linked;

    public Texture2D NullTexture { get; private set; }
    public Effect GenericShader {  get; private set;  }
    public SpriteFont GenericFontBold {  get; private set;  }
    public SpriteFont GenericFontMedium {  get; private set;  }
    public SpriteFont GenericFontRegular {  get; private set;  }

    internal void Initialize()
    {
        var assetIO = new AssetIO();
        
        {
            Color[] nullTexturePixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    nullTexturePixels[y * 32 + x] = (x / 16 ^ y / 16) == 1
                        ? Color.Magenta
                        : Color.Black;

            NullTexture = new Texture2D(main.GraphicsDevice, 32, 32);
            NullTexture.SetData(nullTexturePixels);
        }
        
        {
            var identifier = "generic_shader";
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Assets.Built-In.Shaders.GenericShader.xnb");
            var stream = new MemoryStream(asset.Data);
            GenericShader = Create<Effect>(identifier, stream);  
        }

        {
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Assets.Built-In.Fonts.IBMPlexMono-Bold.xnb");
            CreateFont("font_bold", asset.Data);
            GenericFontBold = GetFont("font_bold");
        }

        {
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Assets.Built-In.Fonts.IBMPlexMono-Medium.xnb");
            CreateFont("font_medium", asset.Data);   
            GenericFontMedium = GetFont("font_medium");
        }

        {
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Assets.Built-In.Fonts.IBMPlexMono-Regular.xnb");
            CreateFont("font_regular", asset.Data);
            GenericFontRegular = GetFont("font_regular");   
        }
    }

    internal void CreateTexture(string identifier, Color[] colors, int X, int Y)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var source = new Texture2D(main.GraphicsDevice, X, Y);
        source.SetData(colors);

        sourceResources[(GraphicResourceType.Texture, identifier)] = source;
    }
    internal void CreateFont(string identifier, byte[] data)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var stream = new MemoryStream(data);
        var source = Create<SpriteFont>(identifier, stream);
        sourceResources[(GraphicResourceType.Font, identifier)] = source;
    }
    internal void CreateShader(string identifier, byte[] data)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var stream = new MemoryStream(data);
        var source = Create<Effect>(identifier, stream);
        sourceResources[(GraphicResourceType.Shader, identifier)] = source;
    }

    internal void UpdateTexture(string identifier, Color[] colors)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if(!sourceResources.TryGetValue((GraphicResourceType.Texture, identifier), out object source))
            throw new KeyNotFoundException($"Failed to find source texture with '{identifier}' identifier.");

        ((Texture2D)source).SetData(colors);
    }
    
    public void UnloadResource(GraphicResourceType resourceType, string identifier)
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
    public void UnloadResources()
    {
        foreach(var key in sourceResources.Keys)
        {
            UnloadResource(key.Item1, key.Item2);
        }
    }

    internal void LinkResource(string identifier, TextureResource resource)
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
    internal void LinkResource(string identifier, FontResource resource)
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
    internal void LinkResource(string identifier, ShaderResource resource)
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

    internal void UnlinkResource(Resource resource)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!linked.ContainsKey(resource))
            return;

        linked.Remove(resource);
    }

    public Texture2D GetTexture(string identifier)
    {
        return sourceResources.TryGetValue((GraphicResourceType.Texture, identifier), out object source) ? (Texture2D)source : NullTexture;
    }
    public Effect GetShader(string identifier)
    {
        return (Effect)sourceResources[(GraphicResourceType.Shader, identifier)];
    }
    public SpriteFont GetFont(string identifier)
    {
        return (SpriteFont)sourceResources[(GraphicResourceType.Font, identifier)];
    }

    public Texture2D GetTexture(TextureResource resource)
    {
        if(resource == null)
            return NullTexture;

        if(linked.TryGetValue(resource, out object source))
            return (Texture2D)source;
        
        Logger.NewLog(Logger.Log.Level.Error, $"TextureResource '{resource.Identifier}' is not linked to a source!");
        return NullTexture;
    }
    public Effect GetShader(ShaderResource resource)
    {
        if(resource == null)
            return GenericShader;

        if(linked.TryGetValue(resource, out object source))
            return (Effect)source;
        
        Logger.NewLog(Logger.Log.Level.Error, $"ShaderResource '{resource.Identifier}' is not linked to a source!");
        return GenericShader;
    }
    public SpriteFont GetFont(FontResource resource)
    {
        return linked.TryGetValue(resource, out object source) ? (SpriteFont)source : throw new KeyNotFoundException($"FontResource '{resource.Identifier}' is not linked to a source!");
    }
    public object GetTextureOrFont(Resource resource)
    {
        return resource is TextureResource texture ? GetTexture(texture) : GetFont((FontResource)resource);
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
    
    [Obsolete("Use UnloadResource(Resource) instead")]
    public sealed override void UnloadAsset(string assetName)
    {
        throw new NotSupportedException(nameof(UnloadAsset));
    }
    [Obsolete("Use UnloadResources() or UnloadResource(GraphicResourceType, string) instead")]
    public sealed override void UnloadAssets(IList<string> assetNames)
    {
        throw new NotSupportedException(nameof(UnloadAssets));
    }
    [Obsolete("Use UnloadResources() or UnloadResource(GraphicResourceType, string) instead")]
    public sealed override void Unload()
    {
        throw new NotSupportedException(nameof(Unload));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        disposed = true;
    }
}