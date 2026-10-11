using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.IO;

namespace Luminix;

internal sealed class GraphicsResourceManager : Microsoft.Xna.Framework.Content.ContentManager
{
    internal GraphicsResourceManager(MainRender main) : base(main.Services)
    {
        this.main = main;

        this.shaders = [];
        this.textures = [];
    }

    public new string RootDirectory => AssetIO.ASSET_DIRECTORY;

    private MainRender main;
    private bool disposed = false;

    private Stream openedStream = null; 

    private readonly Dictionary<ShaderID, Effect> shaders;
    private readonly Dictionary<TextureID, Texture2D> textures;

    public Texture2D NullTexture { get; private set; }
    public Effect GenericShader {  get; private set;  }

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
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Asset.GenericShader.xnb");
            var stream = new MemoryStream(asset.Data);
            GenericShader = Create<Effect>(0, stream);  
        }
    }

    public Texture2D CreateTexture(TextureID texture, Color[] colors, int X, int Y)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var source = new Texture2D(main.GraphicsDevice, X, Y);
        source.SetData(colors);

        textures[texture] = source;
        return source;
    }
    public Effect CreateShader(ShaderID shader, byte[] data)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var stream = new MemoryStream(data);
        var source = Create<Effect>(shader.ID, stream);
        shaders[shader] = source;
        return source;
    }

    public void UpdateTexture(TextureID texture, Color[] colors)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if(!textures.TryGetValue(texture, out Texture2D source))
            throw new KeyNotFoundException($"Failed to find source texture with '{texture.ID}' identifier.");

        ((Texture2D)source).SetData(colors);
    }
    
    public void UnloadTexture(TextureID texture)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if(!textures.TryGetValue(texture, out Texture2D source))
            return;
        
        source.Dispose();
        textures.Remove(texture);
    }
    public void UnloadShader(ShaderID shader)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if(!shaders.TryGetValue(shader, out Effect source))
            return;
            
        source.Dispose();
        shaders.Remove(shader);
    }
    public void UnloadResources()
    {
        foreach(var shader in shaders.Keys)
            UnloadShader(shader);
        foreach(var texture in textures.Keys)
            UnloadTexture(texture);
    }


    internal Effect GetShader(ShaderID resource)
    {
        if(shaders.TryGetValue(resource, out Effect source))
            return (Effect)source;
        
        return GenericShader;
    }
    internal Texture2D GetTexture(TextureID resource)
    {
        if(textures.TryGetValue(resource, out Texture2D source))
            return (Texture2D)source;
        
        return NullTexture;
    }

    private T Create<T>(uint id, Stream stream) where T : class
    {
        T result = null;

        try
        {
            openedStream = stream;
            result = ReadAsset<T>("resource", null);
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