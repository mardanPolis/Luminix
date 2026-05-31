using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Collections;
using Luminix.Identifiers;
using Luminix.IO;
using System.Collections.Immutable;
using Microsoft.Xna.Framework.Audio;

namespace Luminix.Resources;

public sealed class ResourceManager : Microsoft.Xna.Framework.Content.ContentManager
{
    private struct Textures
    {
        private struct SourceTexture
        {
            public SourceTexture(string name, Texture2D texture)
            {
                ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
                ArgumentNullException.ThrowIfNull(texture, nameof(texture));

                Name = name;
                Texture = texture;
                RegionNames = new DenseList<string>();
            }

            public readonly string Name;
            public readonly Texture2D Texture;
            public readonly DenseList<string> RegionNames;
        }

        public Textures()
        {
            sourceTextures = [];
        }

        private readonly Dictionary<string, SourceTexture> sourceTextures;

        public void NewSource(string name, Texture2D texture)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
            ArgumentNullException.ThrowIfNull(texture, nameof(texture));

            if(sourceTextures.ContainsKey(name))
                throw new ArgumentException($"A texture source with the name '{name}' already exists.");

            sourceTextures.Add(name, new SourceTexture(name, texture));
        }

        public void NewRegion(string sourceName, string name, out Texture2D texture)
        {
            if(!sourceTextures.TryGetValue(sourceName, out SourceTexture source))
                throw new ArgumentException($"There is no sourceTexture registered with name {sourceName}.");
            
            source.RegionNames.Add(name);
            texture = source.Texture;
        }
        public void NewRegion(string sourceName, string name)
        {
            NewRegion(sourceName, name, out _);
        }

        public bool Remove(string name, out DenseList<string> danglingRegions)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));

            danglingRegions = null;

            if(!sourceTextures.TryGetValue(name, out SourceTexture source))
                return false;

            danglingRegions = source.RegionNames;
            sourceTextures.Remove(name);

            return true;
        }
    }

    internal ResourceManager(Main main) : base(main.Services)
    {
        MethodInfo method = typeof(Microsoft.Xna.Framework.Content.ContentManager).GetMethod("RecordDisposable", BindingFlags.Instance | BindingFlags.NonPublic) ??
         throw new InvalidOperationException("Could not find method RecordDisposable on ContentManager.");
        RecordDisposable = (Action<IDisposable>)Delegate.CreateDelegate(typeof(Action<IDisposable>), this, method);

        this.main = main;

        this.textures = new Textures();
        this.resources = [];
    }

    public new string RootDirectory => AssetIO.ASSET_DIRECTORY;

    public IReadOnlyDictionary<string, Resource> Resources => resources;

    Action<IDisposable> RecordDisposable;
    private Main main;

    private Stream openedStream = null; 
    private bool disposed = false;

    private Textures textures;

    private ImmutableDictionary<string, Resource> resources;

    public void CreateResource(Asset asset)
    {
        if(asset is TextureAsset textureAsset)
        {
            CreateTexture(textureAsset.Name, textureAsset.Colors, textureAsset.Size);
            CreateTextureRegion(textureAsset.Name, textureAsset.Name, new Rectangle(0, 0, (int)textureAsset.Size.X, (int)textureAsset.Size.Y));
        }
        else if(asset is AtlasAsset atlasAsset)
        {
            CreateTexture(atlasAsset.Name, atlasAsset.Colors, atlasAsset.Size);
            for(int i = 0; i < atlasAsset.Regions.Length; i++)
            {
                var region = atlasAsset.Regions[i];
                CreateTextureRegion(atlasAsset.Name, region.Name, region.Rectangle);
            }
        }
        else if(asset is AudioAsset audioAsset)
        {
            CreateAudio(audioAsset.Name, audioAsset.SoundEffect);
        }
        else if(asset is StreamAsset streamAsset)
        {
            switch(streamAsset.Type)
            {
                case StreamAsset.AssetType.Texture:
                    CreateTexture(streamAsset.Name, streamAsset.Stream);
                break;
                case StreamAsset.AssetType.Font:
                    CreateFont(streamAsset.Name, streamAsset.Stream);
                break;
                case StreamAsset.AssetType.Audio:
                    CreateAudio(streamAsset.Name, streamAsset.Stream);
                break;
                case StreamAsset.AssetType.Shader:
                    CreateShader(streamAsset.Name, streamAsset.Stream);
                break;
            }
        }
    }

    public void CreateTexture(string name, Color[] colors, Vector2 size)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));
        
        Texture2D texture = new Texture2D(main.GraphicsDevice, (int)size.X, (int)size.Y);
        texture.SetData(colors);

        textures.NewSource(name, texture);
        RecordDisposable(texture);

    }
    public void CreateTexture(string name, Stream stream)
    {
        var source = Create<Texture2D>(name, stream);
        textures.NewSource(name, source);
        textures.NewRegion(name, name);
        var resource = new TextureResource(source, name, 0, 0, source.Width, source.Height);
    }
    public void CreateTextureRegion(string textureName, string name, Rectangle rectangle)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(textureName, nameof(textureName));

        textures.NewRegion(textureName, name, out Texture2D texture);
        TextureResource resource = new TextureResource(texture, name, rectangle);
        resources = resources.Add(name, resource);
    }
    public void CreateAudio(string name, SoundEffect soundEffect)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(soundEffect, nameof(soundEffect));

        AudioResource resource = new AudioResource(soundEffect, name);
        resources = resources.Add(name, resource);

        RecordDisposable(soundEffect);
    }
    public void CreateAudio(string name, Stream stream)
    {
        var source = Create<SoundEffect>(name, stream);
        var resource = new AudioResource(source, name);
        resources = resources.Add(name, resource);
    }
    public void CreateFont(string name, Stream stream)
    {
        var source = Create<SpriteFont>(name, stream);
        var resource = new FontResource(source, name);
        resources = resources.Add(name, resource);
    }
    public void CreateShader(string name, Stream stream)
    {
        var source = Create<Effect>(name, stream);
        var resource = new ShaderResource(source, name);
        resources = resources.Add(name, resource);
    }


    private T Create<T>(string name, Stream stream)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));
        
        if (disposed)
            throw new ObjectDisposedException("ResourceManager");

        if (resources.ContainsKey(name))
            throw new ArgumentException($"A resource with the name '{name}' already exists.", nameof(name));

        try
        {
            openedStream = stream;
            return ReadAsset<T>(name, null);
        }
        finally
        {
            openedStream = null;
        }
    }

    protected sealed override Stream OpenStream(string relativePath)
    {
        if(openedStream != null)
            return openedStream;

        return base.OpenStream(relativePath);
    }

    public sealed override void UnloadAsset(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException("assetName");
        }

        if (disposed)
        {
            throw new ObjectDisposedException("ResourceManager");
        }

        if(!resources.TryGetValue(name, out var resource))
            return;
        
        resources = resources.Remove(name);

        base.UnloadAsset(name);
    }

    public sealed override void Unload() 
    {
        base.Unload();
        resources = resources.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        disposed = true;
    }
}


// NullTexture = LoadTexture("null_texture.png");
// NullTextureRegion = NewTextureRegion("null_texture", "null_texture.png", 0, 0, 32, 32);

// GenericFontBold = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Bold");
// GenericFontMedium = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Medium");
// GenericFontRegular = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Regular");

// TileShader = ContentManager.Load<Effect>("Effects/SpriteShader");