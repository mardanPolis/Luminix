using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.IO;
using Microsoft.Xna.Framework.Audio;
using Luminix.Collections;
using Luminix.Extensions;
using System.Diagnostics;

namespace Luminix;

public sealed class ResourceManager : Microsoft.Xna.Framework.Content.ContentManager
{
    internal ResourceManager(MainRender main) : base(main.Services)
    {
        MethodInfo method = typeof(Microsoft.Xna.Framework.Content.ContentManager).GetMethod("RecordDisposable", BindingFlags.Instance | BindingFlags.NonPublic) ??
         throw new InvalidOperationException("Could not find method RecordDisposable on ContentManager.");
        this.recordDisposable = (Action<IDisposable>)Delegate.CreateDelegate(typeof(Action<IDisposable>), this, method);

        this.logger = Logger.Get();
        this.main = main;

        this.sourceTextures = [];
        this.sourceTextureResources = [];

        this.resources = [];
    }

    public new string RootDirectory => AssetIO.ASSET_DIRECTORY;
    private Action<IDisposable> recordDisposable;

    private Logger logger;
    private MainRender main;
    private bool disposed = false;

    private Stream openedStream = null; 

    private Dictionary<string, Texture2D> sourceTextures;
    private Dictionary<Texture2D, DenseList<string>> sourceTextureResources;

    private Dictionary<(Type, string), Resource> resources;

    private Stopwatch stopwatch = new Stopwatch();

    //TODO : make 2 separate resource handles one for RenderThread other more lightweight for logical

    public void CreateResource(Asset asset)
    {
        stopwatch.Restart();

        if (disposed)
            throw new ObjectDisposedException(nameof(ResourceManager), "Unable to create a Resource: ResourceManager is disposed.");

        if (asset == null)
            throw new ArgumentNullException(nameof(asset), "Unable to create a Resource: provided asset cannot be null.");

        if (string.IsNullOrWhiteSpace(asset.Identifier))
            throw new ArgumentException($"Unable to create a Resource: asset identifier is null, empty, or whitespace ('{asset.Identifier}').", nameof(asset));

        if (asset.Identifier.HasWhitespace())
            throw new ArgumentException($"Unable to create a Resource: asset identifier cannot contain whitespace ('{asset.Identifier}').", nameof(asset));

        
        if(asset is TextureAsset textureAsset)
        {
            CreateFrom(textureAsset);
            return;
        }
        else if(asset is AtlasAsset atlasAsset)
        {
            CreateFrom(atlasAsset);
            return;
        }
        else if(asset is AudioAsset audioAsset)
        {
            CreateFrom(audioAsset);
            return;
        }
        else if(asset is XnbAsset xnbAsset)
        {
            CreateFrom(xnbAsset);
            return;
        }

        throw new NotSupportedException($"Unable to create a Resource: unknown asset type '{asset.GetType().Name}'.");
    }

    private void CreateFrom(TextureAsset asset)
    {
        var assetIdentifier = asset.Identifier.ToLower();

        var textureSize = asset.Size;
        var textureColors = asset.Colors;

        if (textureColors == null)
            throw new ArgumentException("Unable to create a TextureResource: provided texture colors cannot be null.", nameof(asset));

        if (sourceTextures.ContainsKey(assetIdentifier))
            throw new InvalidOperationException($"Unable to create a TextureResource: a SourceTexture is already defined with identifier '{assetIdentifier}'.");

        if (resources.ContainsKey((typeof(TextureResource), assetIdentifier)))
            throw new InvalidOperationException($"Unable to create a TextureResource: a TextureResource is already defined with identifier '{assetIdentifier}'.");

        var texture = CreateSourceTexture(assetIdentifier, textureColors, textureSize);
        var resource = CreateTexture(texture, assetIdentifier, new Rectangle(0, 0, (int)asset.Size.X, (int)asset.Size.Y));
        var resourceIdentifiers = new DenseList<string>();

        sourceTextures[assetIdentifier] = texture;
        sourceTextureResources[texture] = resourceIdentifiers;

        resourceIdentifiers.Add(assetIdentifier);
    }
    private void CreateFrom(AtlasAsset asset)
    {
        var assetIdentifier = asset.Identifier.ToLower();

        var atlasColors = asset.Colors;
        var atlasRegions = asset.Regions;
        var atlasSize = asset.Size;

        if (atlasColors == null)
            throw new ArgumentException("Unable to create an Atlas: provided atlas colors cannot be null.", nameof(asset));

        if (atlasRegions == null)
            throw new ArgumentException("Unable to create an Atlas: provided atlas regions cannot be null.", nameof(asset));

        if (sourceTextures.ContainsKey(assetIdentifier))
            throw new InvalidOperationException($"Unable to create an Atlas: a SourceTexture is already defined with identifier '{assetIdentifier}'.");


        var regions = new List<AtlasAsset.Region>();

        for (int i = 0; i < atlasRegions.Length; i++)
        {
            var region = asset.Regions[i];
            var regionIdentifier = region.Identifier.ToLower();

            if (string.IsNullOrWhiteSpace(regionIdentifier))
                throw new ArgumentException($"Unable to create a TextureResource: atlas.regions[{i}].identifier is null, empty, or whitespace.", nameof(asset));

            if (regionIdentifier.HasWhitespace())
                throw new ArgumentException($"Unable to create a TextureResource: atlas.regions[{i}].identifier cannot contain whitespace ('{regionIdentifier}').", nameof(asset));

            if (resources.ContainsKey((typeof(TextureResource), assetIdentifier)))
                throw new InvalidOperationException($"Unable to create a TextureResource: a TextureResource is already defined with identifier '{regionIdentifier}'.");

            regions.Add(region);
        }

        var texture = CreateSourceTexture(assetIdentifier, atlasColors, atlasSize);
        var resourceIdentifiers = new DenseList<string>();
        var created = new Resource[regions.Count];

        sourceTextures[assetIdentifier] = texture;
        sourceTextureResources[texture] = resourceIdentifiers;

        for (int i = 0; i < regions.Count; i++)
        {
            var region = regions[i];
            resourceIdentifiers.Add(region.Identifier);
            created[i] = CreateTexture(texture, region.Identifier, region.Rectangle);
        }
    }
    private void CreateFrom(AudioAsset asset)
    {
        var assetIdentifier = asset.Identifier.ToLower();

        if (asset.SoundEffect == null)
            throw new ArgumentException("Unable to create an AudioResource: provided audio soundEffect cannot be null.", nameof(asset));

        if (resources.ContainsKey((typeof(AudioResource), assetIdentifier)))
            throw new InvalidOperationException($"Unable to create a AudioResource: a AudioResource is already defined with identifier '{assetIdentifier}'.");
            
        CreateAudio(assetIdentifier, asset.SoundEffect);
    }
    private void CreateFrom(XnbAsset asset)
    {
        var assetIdentifier = asset.Identifier.ToLower();

        switch (asset.Type)
        {
            case XnbAsset.XnbType.Font:
                if (asset.Stream == null)
                    throw new ArgumentException("Unable to create a FontResource: provided xnb stream cannot be null.", nameof(asset));

                if (resources.ContainsKey((typeof(FontResource), assetIdentifier)))
                    throw new InvalidOperationException($"Unable to create a FontResource: a FontResource is already defined with identifier '{assetIdentifier}'.");

                CreateFont(assetIdentifier, asset);
            break;
            case XnbAsset.XnbType.Shader:
                if (asset.Stream == null)
                    throw new ArgumentException("Unable to create a ShaderResource: provided xnb stream cannot be null.", nameof(asset));

                if (resources.ContainsKey((typeof(ShaderResource), assetIdentifier)))
                    throw new InvalidOperationException($"Unable to create a ShaderResource: a ShaderResource is already defined with identifier '{assetIdentifier}'.");

                CreateShader(assetIdentifier, asset);
            break;
            default:
                throw new NotSupportedException($"Unable to create a Resource: unrecognized XnbType '{asset.Type}'.");
        }
    }

    private Texture2D CreateSourceTexture(string identifier, Color[] colors, Vector2 size)
    {
        Texture2D texture = new Texture2D(main.GraphicsDevice, (int)size.X, (int)size.Y);
        texture.SetData(colors);

        sourceTextures[identifier] = texture;
        recordDisposable(texture);
        return texture;
    }
    
    private TextureResource CreateTexture(Texture2D texture, string identifier, Rectangle rectangle)
    {
        TextureResource resource = new TextureResource(texture, identifier, rectangle);
        resources.Add((typeof(TextureResource), identifier), resource);
        return resource;
    }
    private AudioResource CreateAudio(string identifier, SoundEffect source)
    {
        AudioResource resource = new AudioResource(source, identifier);
        resources.Add((typeof(AudioResource), identifier), resource);
        recordDisposable(source);
        return resource;
    }
    private FontResource CreateFont(string identifier, XnbAsset xnbAsset)
    {
        var source = Create<SpriteFont>(identifier, xnbAsset.Stream);
        var resource = new FontResource(source, identifier);
        resources.Add((typeof(FontResource), identifier), resource);
        return resource;
    }
    private ShaderResource CreateShader(string identifier, XnbAsset xnbAsset)
    {
        var source = Create<Effect>(identifier, xnbAsset.Stream);
        var resource = new ShaderResource(source, identifier);
        resources.Add((typeof(ShaderResource), identifier), resource);
        recordDisposable(source);
        return resource;
    }

    public T GetResource<T>(string identifier) where T : Resource
    {
        identifier = identifier.ToLower();
        Type type = typeof(T);

        if (resources != null && resources.TryGetValue((type, identifier), out Resource resource))
            return (T)resource;

        Logger.Get().NewLog(Logger.Log.Level.Warning, $"Resource {typeof(T)}, identified '{identifier}' was not found.");
        return null;
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
    
    public void UnloadAsset(Resource resource)
    {
        ObjectDisposedException.ThrowIf(disposed, "ResourceManager");
        ArgumentNullException.ThrowIfNull(resource, nameof(resource));

        if(!resources.Remove((resource.GetType(), resource.Identifier)))
            return;
    }

    public sealed override void Unload() 
    {
        foreach(Resource resource in resources.Values)
        {
            UnloadAsset(resource);
        }

        resources.Clear();
    }

    public sealed override void UnloadAsset(string assetName)
    {
        throw new NotSupportedException(nameof(UnloadAsset));
    }

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