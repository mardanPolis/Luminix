using System;
using System.Collections;
using System.Collections.Generic;
using Luminix.Collections;
using Luminix.Threading.Bridges;
using Luminix.Threading.Commands;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class Resources : Workspace.Service, IEnumerable<Resource>
{
    internal Resources() 
    {
        container = new();
        resourceBridge = ThreadBridges.GetBridge<CommandBridge<GraphicResourceCommand>>(Application.RENDER_THREAD_NAME);
    }

    private CommandBridge<GraphicResourceCommand> resourceBridge;
    private Dictionary<(Type, string), Resource> container;
    public int ResourceCount => container.Count;
 
    internal ShaderResource DefaultShader;
    internal TextureResource DefaultTexture;
    internal FontResource DefaultFont;

    private ShaderID sourceShader;
    private TextureID sourceTexture;

    internal void LoadDefaults()
    {
        var assetIO = Workspace.GetService<Assets>().GetAssetIO();

        {
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Asset.GenericShader.xnb");
            ProcessShaderAsset("generic_shader", asset.Data);
            DefaultShader = GetResource<ShaderResource>("generic_shader");
        }

        {
            Color[] colors = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    colors[y * 32 + x] = (x / 16 ^ y / 16) == 1
                        ? Color.Magenta
                        : Color.Black;
            
            var size = new Point(32, 32);
            ProcessTextureAsset("null", colors, size);
            DefaultTexture = GetResource<TextureResource>("null");
        }

        {
            var identifier = "font_bold";
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Asset.IBMPlexMono-Bold.xnb");
            ProcessFontAsset(identifier, asset.Data);
        }

        {
            var identifier = "font_medium";
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Asset.IBMPlexMono-Medium.xnb");
            ProcessFontAsset(identifier, asset.Data);
            DefaultFont = GetResource<FontResource>("font_medium");
        }

        {
            var identifier = "font_regular";
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Asset.IBMPlexMono-Regular.xnb");
            ProcessFontAsset(identifier, asset.Data);
        }
        
        var graphics = Workspace.GetService<Graphics>();

        graphics.DefaultShader = DefaultShader;
        graphics.DefaultTexture = DefaultTexture;
        graphics.DefaultFont = DefaultFont;

        Logger.NewLog(Logger.Log.Level.Info, "Graphics Default Resources set");
    }

    public void CreateResource(Asset asset)
    {
        CreateResource(asset, asset.Identifier);
    }

    public void CreateResource(Asset asset, string identifier)
    {
        identifier = identifier.Replace('\\', '/').ToLower();
        switch (asset)
        {
            case AudioAsset audioAsset:
                ProcessAudioAsset(identifier, audioAsset);
                break;
                
            case TextureAsset textureAsset:
                ProcessTextureAsset(identifier, textureAsset.Colors, textureAsset.Size);
                break;
                
            case AtlasAsset atlasAsset:
                ProcessAtlasAsset(identifier, atlasAsset.Colors, atlasAsset.Size, atlasAsset.Regions);
                break;
                
            case XnbAsset xnbAsset when xnbAsset.Type == XnbAsset.XnbType.Font:
                ProcessFontAsset(identifier, xnbAsset.Data);
                break;
                
            case XnbAsset xnbAsset:
                ProcessShaderAsset(identifier, xnbAsset.Data);
                break;
                
            default:
                throw new ArgumentException($"Unsupported asset type '{asset.GetType()}'", nameof(asset));
        }
    }
    public T GetResource<T>(string identifier) where T : Resource
    {
        identifier = identifier.Replace('\\', '/').ToLower();
        Type type = typeof(T);

        if (container.TryGetValue((type, identifier), out Resource resource))
            return (T)resource;

        Logger.NewLog(Logger.Log.Level.Warning, $"Resource identified '{typeof(T)}' '{identifier}' was not found.");
        return null;
    }
    public void UnloadResource(Resource resource)
    {
        switch (resource)
        {
            case AudioResource audio:
                if (!container.Remove((typeof(AudioResource), audio.Identifier))) return;
                Workspace.GetService<AudioMedia>().DisposeClip(audio);
                break;

            case TextureResource texture:
            {
                if(resource == DefaultTexture)
                {
                    Logger.NewLog(Logger.Log.Level.Warning,$"Cannot unload texture '{texture.Identifier}', reason: The provided Texture is Default.");
                    return;
                }

                if (!texture.IsSource)           // region: owner decides
                {
                    Logger.NewLog(Logger.Log.Level.Warning,
                        $"Cannot unload region '{texture.Identifier}' individually; unload its atlas.");
                    return;
                }
                if (!container.Remove((typeof(TextureResource), texture.Identifier))) return;

                var regions = texture.SubTextures;    // your DenseList
                for (int i = 0; i < regions.Count; i++)
                {
                    container.Remove((typeof(TextureResource), regions[i].Identifier));
                }
                
                resourceBridge.NewCommand(GraphicResourceCommand.Unload(texture.Source.ID, GraphicResourceType.Texture));
                break;
            }

            case ShaderResource shader:
                if(resource == DefaultShader)
                {
                    Logger.NewLog(Logger.Log.Level.Warning,$"Cannot unload Shader '{shader.Identifier}', reason: The provided Shader is Default.");
                    return;
                }

                if (!container.Remove((typeof(ShaderResource), shader.Identifier))) return;
                resourceBridge.NewCommand(GraphicResourceCommand.Unload(shader.Source.ID, GraphicResourceType.Shader));
                break;

            case FontResource font:
                if(resource == DefaultFont)
                {
                    Logger.NewLog(Logger.Log.Level.Warning,$"Cannot unload Font '{font.Identifier}', reason: The provided Font is Default.");
                    return;
                }

                if (!container.Remove((typeof(FontResource), font.Identifier))) return;
                UnloadResource(font.Atlas);
                break;

            default:
                throw new ArgumentException($"Unsupported resource type '{resource.GetType()}'", nameof(resource));
        }
    }

    private void ProcessAudioAsset(string identifier, AudioAsset audioAsset)
    {
        if (container.ContainsKey((typeof(AudioResource), identifier)))
            throw new InvalidOperationException($"Audio resource with identifier '{identifier}' already exists");

        var clip = Workspace.GetService<AudioMedia>().GetClip(audioAsset);
        var resource = new AudioResource(clip, identifier);
        container.Add((typeof(AudioResource), identifier), resource);
    }
    private void ProcessTextureAsset(string identifier, Color[] colors, Point size)
    {
        if (container.ContainsKey((typeof(TextureResource), identifier)))
            throw new InvalidOperationException($"Texture resource with identifier '{identifier}' already exists");

        var source = new TextureID(sourceTexture.ID++);

        var resource = new TextureResource(source, identifier, 0, 0, size.X, size.Y, []);
        container.Add((resource.GetType(), identifier), resource);

        var sourceCreateCommand = GraphicResourceCommand.Create(source.ID, colors, size.X, size.Y);
        resourceBridge.NewCommand(sourceCreateCommand);
    }
    private void ProcessAtlasAsset(string identifier, Color[] colors, Point size, AtlasAsset.Region[] regions)
    {
        if (container.ContainsKey((typeof(TextureResource), identifier)))
            throw new InvalidOperationException($"Texture resource with identifier '{identifier}' already exists");

        var source = new TextureID(sourceTexture.ID++);

        var subTextures = new DenseList<TextureResource>(regions.Length);
        var resource = new TextureResource(source, identifier, 0, 0, size.X, size.Y, subTextures);
        container.Add((resource.GetType(), identifier), resource);

        // Source commands
        var sourceCreateCommand = GraphicResourceCommand.Create(source.ID, colors, size.X, size.Y);
        resourceBridge.NewCommand(sourceCreateCommand);

        for (int i = 0; i < regions.Length; i++)
        {
            var region = regions[i];
            var regionIdentifier = region.Identifier.Replace('\\', '/').ToLower();
            var regionRect = region.Rectangle;

            if (container.ContainsKey((typeof(TextureResource), regionIdentifier)))
            {
                Logger.NewLog(Logger.Log.Level.Warning, $"Unable to create atlas region, reason: Texture resource with identifier '{regionIdentifier}' already exists.");
                continue;
            }

            var regionResource = new TextureResource(source, regionIdentifier, regionRect);
            container.Add((regionResource.GetType(), regionIdentifier), regionResource);

            subTextures.Add(regionResource);
        }
    }
    private void ProcessFontAsset(string identifier, byte[] data)
    {
        if (container.ContainsKey((typeof(FontResource), identifier)))
            throw new InvalidOperationException($"Font resource with identifier '{identifier}' already exists");

        var parsed = XnbFontParser.Parse(data);
        var glyphInfos = parsed.Glyphs;

        var atlasIdentifier = identifier + "_atlas";
        if (container.ContainsKey((typeof(TextureResource), atlasIdentifier)))
            throw new InvalidOperationException($"Texture resource with identifier '{atlasIdentifier}' already exists");

        var source = new TextureID(sourceTexture.ID++);

        var subTextures = new DenseList<TextureResource>(glyphInfos.Length);
        var atlas = new TextureResource(source, atlasIdentifier, 0, 0, parsed.AtlasWidth, parsed.AtlasHeight, subTextures);
        container.Add((typeof(TextureResource), atlasIdentifier), atlas);
        
        resourceBridge.NewCommand(GraphicResourceCommand.Create(source.ID, parsed.AtlasPixels, parsed.AtlasWidth, parsed.AtlasHeight));

        var regions = new List<TextureResource>(glyphInfos.Length);
        for (int i = 0; i < glyphInfos.Length; i++)
        {
            var glyph = glyphInfos[i];
            if (glyph.Bounds.Width <= 0 || glyph.Bounds.Height <= 0)
                continue;

            var regionIdentifier = atlasIdentifier + "/" + (int)glyph.Character;
            if (container.TryGetValue((typeof(TextureResource), regionIdentifier), out var existing))
            {
                regions.Add((TextureResource)existing);
                continue;
            }

            var region = new TextureResource(source, regionIdentifier, glyph.Bounds);
            container.Add((typeof(TextureResource), regionIdentifier), region);
            subTextures.Add(region);
            regions.Add(region);
        }

        var font = new FontResource(identifier, parsed, atlas, regions.ToArray());
        container.Add((typeof(FontResource), identifier), font);
    }
    private void ProcessShaderAsset(string identifier, byte[] data)
    {
        if (container.ContainsKey((typeof(ShaderResource), identifier)))
            throw new InvalidOperationException($"Shader resource with identifier '{identifier}' already exists");

        var source = new ShaderID(sourceShader.ID++);

        Resource resource = new ShaderResource(source, identifier);
        container.Add((resource.GetType(), identifier), resource);

        var sourceCreateCommand = GraphicResourceCommand.Create(source.ID, data);
        resourceBridge.NewCommand(sourceCreateCommand);
    }

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<Resource> IEnumerable<Resource>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator: IEnumerator<Resource>
    {
        public Enumerator(Resources resources)
        {
            this.resources = resources;
            Reset();
        }

        private Resources resources;
        private Dictionary<(Type, string), Resource>.Enumerator inner;
        private Resource current;

        public Resource Current => current;
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if(inner.MoveNext())
            {
                current = inner.Current.Value;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            current = default;
            inner = resources.container.GetEnumerator();
        }

        public void Dispose()
        {
            resources = null;
            current = null;
        }
    }

}