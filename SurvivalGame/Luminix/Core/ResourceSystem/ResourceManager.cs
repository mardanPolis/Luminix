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

namespace Luminix;

public sealed class ResourceManager : Microsoft.Xna.Framework.Content.ContentManager
{
    internal ResourceManager(Main main) : base(main.Services)
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
    private Main main;
    private Resources resources;
    private bool disposed = false;

    private Stream openedStream = null; 

    private Dictionary<string, Texture2D> sourceTextures;
    private Dictionary<Texture2D, DenseList<string>> sourceTextureResources;

    private Dictionary<string, TextureResource> textureResources;
    private Dictionary<string, AudioResource> audioResources;
    private Dictionary<string, FontResource> fontResources;
    private Dictionary<string, ShaderResource> shaderResources;

    public void CreateResource(Asset asset)
    {
        using var _ = logger.WithPrefix("ResourceManager - Unable to create a new Resource, reason: ");

        bool flag1 = disposed;
        bool flag2 = asset == null;
        bool flag3 = string.IsNullOrWhiteSpace(asset.Identifier);
        bool flag4 = asset.Identifier.Any(char.IsWhiteSpace);

        try
        {
            if(logger.ErrIf(flag1, $"ResourceManager is disposed!")) return;
            if(logger.ErrIf(flag2, $"Provided asset cannot be null!")) return;
            if(logger.ErrIf(flag3, $"Provided asset identifier is null, empty or is composed entirely of whitespace, '{asset.Identifier}'")) return;
            if(logger.ErrIf(flag4, $"Provided asset identifier cannot contain whitespace, '{asset.Identifier}'")) return;

            var assetIdentifier = asset.Identifier.ToLower();

            if(asset is TextureAsset textureAsset)
            {
                var textureSize = textureAsset.Size;
                var textureColors = textureAsset.Colors;

                flag1 = textureColors == null;
                flag2 = sourceTextures.ContainsKey(assetIdentifier);
                flag3 = textureResources.ContainsKey(assetIdentifier);

                if(logger.ErrIf(flag1, $"Provided texture colors cannot be null!")) return;
                if(logger.ErrIf(flag2, $"A SourceTexture is already defined with identifier '{assetIdentifier}'")) return;
                if(logger.WrnIf(flag3, $"A TextureResource is already defined with identifier '{assetIdentifier}'")) return;

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

                flag1 = atlasColors == null;
                flag2 = atlasRegions == null;
                flag3 = sourceTextures.ContainsKey(assetIdentifier);

                if(logger.ErrIf(flag1, $"Provided atlas colors cannot be null!")) return;
                if(logger.ErrIf(flag2, $"Provided atlas regions cannot be null!")) return;
                if(logger.ErrIf(flag3, $"A SourceTexture is already defined with identifier '{assetIdentifier}'")) return;

                var regions = new List<AtlasAsset.Region>();

                for(int i = 0; i < atlasRegions.Length; i++)
                {
                    var region = atlasAsset.Regions[i];
                    var regionIdentifier = region.Identifier.ToLower();
 
                    flag1 = string.IsNullOrWhiteSpace(regionIdentifier);
                    flag2 = regionIdentifier.Any(char.IsWhiteSpace);
                    flag3 = textureResources.ContainsKey(regionIdentifier);

                    if(logger.ErrIf(flag1, $"atlas.regions[{i}].identifier is null, empty or is composed entirely of whitespace!")) return;
                    if(logger.ErrIf(flag2, $"atlas.regions[{i}].identifier cannot contain whitespace, '{regionIdentifier}'")) return;
                    if(logger.WrnIf(flag3, $"A TextureResource is already defined with identifier '{regionIdentifier}'")) continue;

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
                if(logger.ErrIf(audioAsset.SoundEffect == null, $"Provided audio soundEffect cannot be null!")) return;
                CreateAudio(assetIdentifier, audioAsset.SoundEffect);

                return;
            }
            else if(asset is XnbAsset xnbAsset)
            {
                if(logger.ErrIf(xnbAsset.Stream == null, $"Provided xnb stream cannot be null!")) return;

                if(xnbAsset.Type == XnbAsset.XnbType.Font)
                    CreateFont(assetIdentifier, xnbAsset);
                else if(xnbAsset.Type == XnbAsset.XnbType.Shader)
                    CreateShader(assetIdentifier, xnbAsset);

                return;
            }

            logger.Err($"Unknown asset type '{asset.GetType().Name}'");
        }
        catch(Exception ex)
        {
            logger.Err(ex);
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