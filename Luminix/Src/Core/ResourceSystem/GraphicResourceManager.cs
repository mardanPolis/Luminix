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
        return shaders[resource];
    }
    internal Texture2D GetTexture(TextureID resource)
    {
        return textures[resource];
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