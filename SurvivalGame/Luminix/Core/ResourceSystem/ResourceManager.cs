using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.IO;
using Microsoft.Xna.Framework.Audio;
using Luminix.Collections;
using System.Linq;
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
        this.resources = new Resources();

        this.sourceTextures = [];
        this.sourceTextureResources = [];

        this.textureResources = [];
        this.audioResources = [];
        this.fontResources = [];
        this.shaderResources = [];
    }

    public new string RootDirectory => AssetIO.ASSET_DIRECTORY;
    public Resources Resources => resources;
    public IReadOnlyDictionary<string, TextureResource> TextureResources => textureResources;
    public IReadOnlyDictionary<string, AudioResource> AudioResources => audioResources;
    public IReadOnlyDictionary<string, FontResource> FontResources => fontResources;
    public IReadOnlyDictionary<string, ShaderResource> ShaderResources => shaderResources;

    private Action<IDisposable> recordDisposable;

    private Logger logger;
    private MainRender main;
    private Resources resources;
    private bool disposed = false;

    private Stream openedStream = null; 

    private Dictionary<string, Texture2D> sourceTextures;
    private Dictionary<Texture2D, DenseList<string>> sourceTextureResources;

    private Dictionary<string, TextureResource> textureResources;
    private Dictionary<string, AudioResource> audioResources;
    private Dictionary<string, FontResource> fontResources;
    private Dictionary<string, ShaderResource> shaderResources;

    private Stopwatch stopwatch = new Stopwatch();

    public void CreateResource(Asset asset)
    {
        try
        {
            stopwatch.Restart();
            if(disposed)
            {
                logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a Resource, reason: ResourceManager is disposed!");
                return;
            }
            if(asset == null)
            {
                logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a Resource, reason: Provided asset cannot be null!");
                return;
            }
            if(string.IsNullOrWhiteSpace(asset.Identifier))
            {
                logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a Resource, reason: Provided asset identifier is null, empty or is composed entirely of whitespace, '{asset.Identifier}'");
                return;
            }
            if(asset.Identifier.HasWhitespace())
            {
                logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a Resource, reason: Provided asset identifier cannot contain whitespaces, '{asset.Identifier}'");
                return;
            }
            
            var assetIdentifier = asset.Identifier.ToLower();

            if(asset is TextureAsset textureAsset)
            {
                var textureSize = textureAsset.Size;
                var textureColors = textureAsset.Colors;

                if(textureColors == null)
                {
                    logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a TextureResource, reason: Provided texture colors cannot be null!");
                    return;
                }
                if(sourceTextures.ContainsKey(assetIdentifier))
                {
                    logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a TextureResource, reason: A SourceTexture is already defined with identifier '{assetIdentifier}'");
                    return;
                }
                if(textureResources.ContainsKey(assetIdentifier))
                {
                    logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a TextureResource, reason: A TextureResource is already defined with identifier '{assetIdentifier}'");
                    return;
                }

                var texture = CreateTexture(assetIdentifier, textureColors, textureSize);
                CreateTextureRegion(texture, assetIdentifier, new Rectangle(0, 0, (int)textureAsset.Size.X, (int)textureAsset.Size.Y));
                var resourceIdentifiers = new DenseList<string>();

                sourceTextures[assetIdentifier] = texture;
                sourceTextureResources[texture] = resourceIdentifiers;

                resourceIdentifiers.Add(assetIdentifier);

                return;
            }
            else if(asset is AtlasAsset atlasAsset)
            {
                var atlasColors = atlasAsset.Colors;
                var atlasRegions = atlasAsset.Regions;
                var atlasSize = atlasAsset.Size;

                if(atlasColors == null)
                {
                    logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create an Atlas, reason: Provided atlas colors cannot be null!");
                    return;
                }
                if(atlasRegions == null)
                {
                    logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create an Atlas, reason: Provided atlas regions cannot be null!");
                    return;
                }
                if(sourceTextures.ContainsKey(assetIdentifier))
                {
                    logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create an Atlas, reason: A SourceTexture is already defined with identifier '{assetIdentifier}'");
                    return;
                }

                var regions = new List<AtlasAsset.Region>();

                for(int i = 0; i < atlasRegions.Length; i++)
                {
                    var region = atlasAsset.Regions[i];
                    var regionIdentifier = region.Identifier.ToLower();

                    if(string.IsNullOrWhiteSpace(regionIdentifier))
                    {
                        logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a TextureResource, reason: atlas.regions[{i}].identifier is null, empty or is composed entirely of whitespace!");
                        return;
                    }
                    if(regionIdentifier.HasWhitespace())
                    {
                        logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a TextureResource, reason: atlas.regions[{i}].identifier cannot contain whitespaces, '{regionIdentifier}'");
                        return;
                    }
                    if(textureResources.ContainsKey(regionIdentifier))
                    {
                        logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a TextureResource, reason: A TextureResource is already defined with identifier '{regionIdentifier}'");
                        return;
                    }

                    regions.Add(region);
                }

                //defining
                var texture = CreateTexture(assetIdentifier, atlasColors, atlasSize);
                var resourceIdentifiers = new DenseList<string>();

                sourceTextures[assetIdentifier] = texture;
                sourceTextureResources[texture] = resourceIdentifiers;

                for(int i = 0; i < regions.Count; i++)
                {
                    var region = regions[i];
                    resourceIdentifiers.Add(region.Identifier);
                    CreateTextureRegion(texture, region.Identifier, region.Rectangle);
                }

                return;
            }
            else if(asset is AudioAsset audioAsset)
            {
                if(audioAsset.SoundEffect == null)
                {
                    logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create an AudioResource, reason: Provided audio soundEffect cannot be null!");
                    return;
                }

                CreateAudio(assetIdentifier, audioAsset.SoundEffect);

                return;
            }
            else if(asset is XnbAsset xnbAsset)
            {
                switch(xnbAsset.Type)
                {
                    case XnbAsset.XnbType.Font:
                        if(xnbAsset.Stream == null)
                        {
                            logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a FontResource, reason: Provided xnb stream cannot be null!");
                            return;
                        }

                        CreateFont(assetIdentifier, xnbAsset);
                    break;
                    case XnbAsset.XnbType.Shader:
                        if(xnbAsset.Stream == null)
                        {
                            logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a ShaderResource, reason: Provided xnb stream cannot be null!");
                            return;
                        }
                        
                        CreateShader(assetIdentifier, xnbAsset);
                    break;
                }
                return;
            }

            logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a Resource, reason: Unknown asset type '{asset.GetType().Name}'");
        }
        catch(Exception ex)
        {
            logger.NewLog(Logger.Log.Level.Error, $"ResourceManager - Unable to create a Resource, reason: {ex}'");
        }
    }

        // ArgumentNullException.ThrowIfNull(stream, nameof(stream));

    private Texture2D CreateTexture(string identifier, Color[] colors, Vector2 size)
    {
        Texture2D texture = new Texture2D(main.GraphicsDevice, (int)size.X, (int)size.Y);
        texture.SetData(colors);

        sourceTextures[identifier] = texture;
        recordDisposable(texture);
        return texture;

    }
    private void CreateTextureRegion(Texture2D texture, string identifier, Rectangle rectangle)
    {
        TextureResource resource = new TextureResource(texture, identifier, rectangle);
        textureResources.Add(identifier, resource);
    }
    private void CreateAudio(string identifier, SoundEffect source)
    {
        AudioResource resource = new AudioResource(source, identifier);
        audioResources.Add(identifier, resource);
        recordDisposable(source);
    }

    private void CreateFont(string identifier, XnbAsset xnbAsset)
    {
        var source = Create<SpriteFont>(identifier, xnbAsset.Stream);
        var resource = new FontResource(source, identifier);
        fontResources.Add(identifier, resource);
    }

    private void CreateShader(string identifier, XnbAsset xnbAsset)
    {
        var source = Create<Effect>(identifier, xnbAsset.Stream);
        var resource = new ShaderResource(source, identifier);
        shaderResources.Add(identifier, resource);
        recordDisposable(source);
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
    
    public sealed override void UnloadAsset(string identifier)
    {
        ObjectDisposedException.ThrowIf(disposed, "ResourceManager");
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));

        if(!(textureResources.Remove(identifier) || audioResources.Remove(identifier) || fontResources.Remove(identifier) || shaderResources.Remove(identifier)))
            return;
        
        base.UnloadAsset(identifier);
    }

    public sealed override void Unload() 
    {
        base.Unload();
        textureResources.Clear();
        audioResources.Clear();
        fontResources.Clear();
        shaderResources.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        disposed = true;
    }
}