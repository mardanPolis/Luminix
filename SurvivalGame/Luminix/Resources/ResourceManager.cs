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

    Action<IDisposable> RecordDisposable;
    private Main main;

    private Stream openedStream = null; 
    private bool disposed = false;

    private Textures textures;

    private ImmutableDictionary<string, Resource> resources;

    public void CreateTexture(string name, Color[] colors, Vector2 scale)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));
        
        Texture2D texture = new Texture2D(main.GraphicsDevice, (int)scale.X, (int)scale.Y);
        texture.SetData(colors);

        textures.NewSource(name, texture);
        RecordDisposable(texture);
    }

    public void CreateTextureRegion(string textureName, string name, Rectangle rectangle)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(textureName, nameof(textureName));

        textures.NewRegion(textureName, name, out Texture2D texture);
        TextureResource resource = new TextureResource(texture, name, rectangle);
        resources = resources.Add(name, resource);
    }

    public void Create<T>(string name, Stream stream)
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
            T asset = ReadAsset<T>(name, null);

            Resource resource = null;
            if (asset is Texture2D texture)
            {
                textures.NewSource(name, texture);
                textures.NewRegion(name, name);
                resource = new TextureResource(texture, name, 0, 0, texture.Width, texture.Height);
            }
            else if (asset is Effect effect)
            {
                resource = new ShaderResource(effect, name);
            }
            else if (asset is SpriteFont font)
            {
                resource = new FontResource(font, name);
            }
            else if (asset is SoundEffect soundEffect)
            {
                resource = new AudioResource(soundEffect, name);
            }
            else
            {
                throw new NotSupportedException($"Asset type '{typeof(T)}' is not supported by ResourceManager.");
            }

            resources.Add(name, resource);
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