using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Luminix.Collections;
using Luminix.IO;
using Luminix.Threading.Bridges;
using Luminix.Threading.Commands;
using Microsoft.Xna.Framework;

namespace Luminix;

public sealed class Resources : Workspace.Service, IEnumerable<Resource>
{
    internal Resources()
    {
        container = new();
        audioMedia = Workspace.Current.GetService<AudioMedia>();
        var assetIO = Workspace.Current.GetService<Assets>().GetAssetIO();
        resourceBridge = ThreadBridges.GetBridge<CommandBridge<GraphicResourceCommand>>(Application.RENDER_THREAD_NAME);

        // --- GENERIC RESOURCES ---

        // FONT BOLD
        {
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Assets.Built-In.Fonts.IBMPlexMono-Bold.xnb");
            Resource resource = new FontResource("font_bold");
            container.Add((resource.GetType(), "font_bold"), resource);

            var sourceCreateCommand = GraphicResourceCommand.Create("font_bold", asset.Data, asset.Type);
            resourceBridge.NewCommand(sourceCreateCommand);

            var sourceLinkCommand = GraphicResourceCommand.Link("font_bold", resource);
            resourceBridge.NewCommand(sourceLinkCommand);
        }

        // FONT MEDIUM
        {
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Assets.Built-In.Fonts.IBMPlexMono-Medium.xnb");
            Resource resource = new FontResource("font_medium");
            container.Add((resource.GetType(), "font_medium"), resource);

            var sourceCreateCommand = GraphicResourceCommand.Create("font_medium", asset.Data, asset.Type);
            resourceBridge.NewCommand(sourceCreateCommand);

            var sourceLinkCommand = GraphicResourceCommand.Link("font_medium", resource);
            resourceBridge.NewCommand(sourceLinkCommand);
        }

        // FONT REGULAR
        {
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Assets.Built-In.Fonts.IBMPlexMono-Regular.xnb");
            Resource resource = new FontResource("font_regular");
            container.Add((resource.GetType(), "font_regular"), resource);

            var sourceCreateCommand = GraphicResourceCommand.Create("font_regular", asset.Data, asset.Type);
            resourceBridge.NewCommand(sourceCreateCommand);

            var sourceLinkCommand = GraphicResourceCommand.Link("font_regular", resource);
            resourceBridge.NewCommand(sourceLinkCommand);

        }

        // GENERIC SHADER
        {
            XnbAsset asset = assetIO.LoadXnbAsset(Application.EngineAssembly, "Assets.Built-In.Shaders.GenericShader.xnb");
            Resource resource = new FontResource("generic_shader");
            container.Add((resource.GetType(), "generic_shader"), resource);

            var sourceCreateCommand = GraphicResourceCommand.Create("generic_shader", asset.Data, asset.Type);
            resourceBridge.NewCommand(sourceCreateCommand);

            var sourceLinkCommand = GraphicResourceCommand.Link("generic_shader", resource);
            resourceBridge.NewCommand(sourceLinkCommand);
        }
    }

    private AudioMedia audioMedia;
    private CommandBridge<GraphicResourceCommand> resourceBridge;
    private Dictionary<(Type, string), Resource> container;
    public int ResourceCount => container.Count;
 
    public void CreateResource(Asset asset)
    {
        switch(asset)
        {
            case AudioAsset audioAsset:
                {
                    if(container.ContainsKey((typeof(AudioResource), asset.Identifier)))
                        throw new InvalidOperationException($"Audio resource with identifier '{asset.Identifier}' already exists");

                    var clip = audioMedia.GetClip(audioAsset);
                    var resource = new AudioResource(clip, asset.Identifier);
                    container.Add((typeof(AudioResource), asset.Identifier), resource);
                }
            break;
            case TextureAsset textureAsset:
                {
                    if(container.ContainsKey((typeof(TextureResource), asset.Identifier)))
                        throw new InvalidOperationException($"Texture resource with identifier '{asset.Identifier}' already exists");

                    var identifier = asset.Identifier;
                    var colors = textureAsset.Colors;
                    var size = textureAsset.Size;

                    var resource = new TextureResource(identifier, 0, 0, (int)size.X, (int)size.Y, []);
                    container.Add((resource.GetType(), asset.Identifier), resource);

                    //Source commands
                    var sourceCreateCommand = GraphicResourceCommand.Create(identifier, colors, (int)size.X, (int)size.Y);
                    resourceBridge.NewCommand(sourceCreateCommand);

                    var sourceLinkCommand = GraphicResourceCommand.Link(identifier, resource);
                    resourceBridge.NewCommand(sourceLinkCommand);
                }
            break;
            case AtlasAsset atlasAsset:
                {
                    if(container.ContainsKey((typeof(TextureResource), asset.Identifier)))
                        throw new InvalidOperationException($"Texture resource with identifier '{asset.Identifier}' already exists");

                    var identifier = asset.Identifier;
                    var colors = atlasAsset.Colors;
                    var size = atlasAsset.Size;

                    var subTextures = new DenseList<TextureResource>(atlasAsset.Regions.Length);
                    var resource = new TextureResource(identifier, 0, 0, (int)size.X, (int)size.Y, subTextures);
                    container.Add((resource.GetType(), identifier), resource);

                    //Source commands
                    var sourceCreateCommand = GraphicResourceCommand.Create(identifier, colors, (int)size.X, (int)size.Y);
                    resourceBridge.NewCommand(sourceCreateCommand);

                    var sourceLinkCommand = GraphicResourceCommand.Link(identifier, resource);
                    resourceBridge.NewCommand(sourceLinkCommand);

                    var regions = atlasAsset.Regions;
                    for(int i = 0; i < regions.Length; i++)
                    {
                        var region = regions[i];
                        var regionIdentifier = region.Identifier;
                        var regionRect = region.Rectangle;

                        if(container.ContainsKey((typeof(TextureResource), regionIdentifier)))
                        {
                            Logger.NewLog(Logger.Log.Level.Warning, $"Unable to create atlas region, reason: Texture resource with identifier '{regionIdentifier}' already exists.");
                            continue;
                        }

                        var regionResource = new TextureResource(regionIdentifier, regionRect);
                        container.Add((regionResource.GetType(), regionIdentifier), regionResource);

                        subTextures.Add(regionResource);

                        var regionLinkCommand = GraphicResourceCommand.Link(identifier, regionResource);
                        resourceBridge.NewCommand(regionLinkCommand);
                    }
                }
            break;
            case XnbAsset xnbAsset:
                {
                    switch(xnbAsset.Type)
                    {
                        case XnbAsset.XnbType.Font:
                            if(container.ContainsKey((typeof(FontResource), asset.Identifier)))
                                throw new InvalidOperationException($"Font resource with identifier '{asset.Identifier}' already exists");
                        break;
                        case XnbAsset.XnbType.Shader:
                            if(container.ContainsKey((typeof(ShaderResource), asset.Identifier)))
                                throw new InvalidOperationException($"Shader resource with identifier '{asset.Identifier}' already exists");
                        break;
                    }

                    var identifier = asset.Identifier;
                    var data = xnbAsset.Data;
                    var type = xnbAsset.Type;
                    Resource resource = type switch
                    {
                        XnbAsset.XnbType.Font   => new FontResource(identifier),
                        XnbAsset.XnbType.Shader => new ShaderResource(identifier),
                        _                       => throw new NotSupportedException($"Unsupported xnb type: {type}")
                    };
                    container.Add((resource.GetType(), asset.Identifier), resource);

                    var sourceCreateCommand = GraphicResourceCommand.Create(identifier, data, type);
                    resourceBridge.NewCommand(sourceCreateCommand);

                    var sourceLinkCommand = GraphicResourceCommand.Link(identifier, resource);
                    resourceBridge.NewCommand(sourceLinkCommand);
                }
            break;
            default:
                throw new ArgumentException($"Unsupported asset type '{asset.GetType()}'", nameof(asset));
        }

    }
    public T GetResource<T>(string identifier) where T : Resource
    {
        identifier = identifier.ToLower();
        Type type = typeof(T);

        if (container.TryGetValue((type, identifier), out Resource resource))
            return (T)resource;

        Logger.NewLog(Logger.Log.Level.Warning, $"Resource identified '{identifier}' was not found.");
        return null;
    }
    public void UnloadResource(Resource resource)
    {
        switch (resource)
        {
            case AudioResource audio:
                if (!container.Remove((typeof(AudioResource), audio.Identifier))) return;
                audioMedia.DisposeClip(audio);
                break;

            case TextureResource texture:
            {
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
                    resourceBridge.NewCommand(GraphicResourceCommand.Unlink(regions[i]));
                    container.Remove((typeof(TextureResource), regions[i].Identifier));
                }

                resourceBridge.NewCommand(GraphicResourceCommand.Unlink(texture));
                resourceBridge.NewCommand(GraphicResourceCommand.Unload(texture.Identifier, GraphicResourceType.Texture));
                break;
            }

            case ShaderResource shader:
                if (!container.Remove((typeof(ShaderResource), shader.Identifier))) return;
                resourceBridge.NewCommand(GraphicResourceCommand.Unlink(shader));
                resourceBridge.NewCommand(GraphicResourceCommand.Unload(shader.Identifier, GraphicResourceType.Shader));
                break;

            case FontResource font:
                if (!container.Remove((typeof(FontResource), font.Identifier))) return;
                resourceBridge.NewCommand(GraphicResourceCommand.Unlink(font));
                resourceBridge.NewCommand(GraphicResourceCommand.Unload(font.Identifier, GraphicResourceType.Font));
                break;

            default:
                throw new ArgumentException($"Unsupported resource type '{resource.GetType()}'", nameof(resource));
        }
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
            return inner.MoveNext();
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