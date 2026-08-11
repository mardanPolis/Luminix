using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Luminix.Core.Handlers;
using Luminix.IO;
using Luminix.RegistrySystem;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Luminix;

public sealed class Assets : Workspace.Service
{
    internal Assets(RegistryManager registryManager, WorkerThreadPool workerThreadPool, AssetBridge assetBridge)
    {
        this.registryManager = registryManager;
        this.workerThreadPool = workerThreadPool;
        this.assetIO = new AssetIO();
        this.assetBridge = assetBridge;


        this.textureAssetInfos = new();
        this.audioAssetInfos = new();
        this.shaderAssets = new();
        this.fontAssets = new();

        this.dataAssets = new();
        this.dataHandlers = new();
        this.dataTypes = new();
    }

    private readonly RegistryManager registryManager;
    private readonly WorkerThreadPool workerThreadPool;
    private readonly AssetIO assetIO;
    private readonly AssetBridge assetBridge;

    private readonly SortedDictionary<string, AssetIO.AssetInfo> textureAssetInfos;
    private readonly Dictionary<string, AssetIO.AssetInfo> audioAssetInfos;
    private readonly Dictionary<string, XnbAsset> shaderAssets;
    private readonly Dictionary<string, XnbAsset> fontAssets;
    private readonly List<DataAsset> dataAssets;

    private readonly Dictionary<string, IDataHandler> dataHandlers;
    private readonly Dictionary<string, Type> dataTypes;

    public AssetIO GetAssetIO() => assetIO;

    public void HandleAsset(AssetIO.AssetInfo assetInfo)
    {
        if (assetIO.IsTextureAsset(assetInfo))
        {
            textureAssetInfos[assetInfo.RelativePath] = assetInfo;
        }
        else if(assetIO.IsAudioAsset(assetInfo))
        {
            audioAssetInfos[assetInfo.RelativePath] = assetInfo;
        }
        else if(assetIO.IsXnbAsset(assetInfo))
        {
            var xnbAsset = assetIO.LoadXnbAsset(assetInfo);
            switch (xnbAsset.Type)
            {
                case XnbAsset.XnbType.Font:
                    shaderAssets[xnbAsset.Identifier] = xnbAsset;
                break;
                case XnbAsset.XnbType.Shader:
                    fontAssets[xnbAsset.Identifier] = xnbAsset;
                break;
            }

        }
        else if(assetIO.IsDataAsset(assetInfo))
        {
            DataAsset dataAsset = assetIO.LoadDataAsset(assetInfo);
            if(!dataTypes.ContainsKey(dataAsset.DefType))
                throw new KeyNotFoundException($"Unknown data type '{dataAsset.DefType}' for asset '{assetInfo.FullPath}'.");

            dataAssets.Add(dataAsset);
        }

        throw new NotSupportedException($"Unrecognized asset type for '{assetInfo.FullPath}'.");
    }

    public void HandleAssets(IEnumerable<AssetIO.AssetInfo> assetInfos)
    {
        foreach(var info in assetInfos)
            HandleAsset(info);
    }

    public AtlasAsset[] CreateAtlasAssets(IEnumerable<TextureAsset> textureAssets)
    {
        var atlasGroups = new Dictionary<string, List<TextureAsset>>();

        //Grouping textures
        foreach (var texture in textureAssets)
        {
            string textureName = Path.GetFileName(texture.Identifier.Replace('·', Path.DirectorySeparatorChar));
            string atlasName = texture.Identifier.Substring(0, Math.Max(0, texture.Identifier.Length - textureName.Length - 1));

            if(string.IsNullOrWhiteSpace(atlasName))
                atlasName = textureName;

            if (!atlasGroups.TryGetValue(atlasName, out var group))
            {
                group = new List<TextureAsset>();
                atlasGroups[atlasName] = group;
            }
            group.Add(texture);
        }
        
        //Making Atlases
        var groupKeys   = atlasGroups.Keys.ToArray();
        var groupValues = atlasGroups.Values.ToArray();

        var resultBag = new ConcurrentBag<AtlasAsset>();
        workerThreadPool.ParallelFor(atlasGroups.Count, index =>
        {
            var atlasMaker = new AtlasMaker();
            var atlasName = groupKeys[index];
            var textures  = groupValues[index];

            foreach(var texture in textures)
                atlasMaker.Add(texture);

            var atlasResults = atlasMaker.Pack(atlasName);
            
            for(int i = 0; i < atlasResults.Length; i++)
                resultBag.Add(atlasResults[i]);
        });

        return resultBag.ToArray();
    }

    internal void FinalizeAssets(bool makeAtlases)
    {
        var assets = new List<Asset>();

        var texturesAssets = new ConcurrentBag<TextureAsset>();
        var audioAssets = new ConcurrentBag<Asset>();
        
        var textureTask = workerThreadPool.ParallelForAsync(textureAssetInfos.Values, async info =>
        {
            TextureAsset textureAsset = await assetIO.LoadTextureAssetAsync(info);
            texturesAssets.Add(textureAsset);
        });

        var audioTask = workerThreadPool.ParallelForAsync(audioAssetInfos.Values, async info =>
        {
            AudioAsset audioAsset = await assetIO.LoadAudioAssetAsync(info);
            audioAssets.Add(audioAsset);
        });

        assets.AddRange(shaderAssets.Values);
        assets.AddRange(fontAssets.Values);

        textureTask.Wait();

        if(makeAtlases)
            assets.AddRange(CreateAtlasAssets(texturesAssets));
        else
            assets.AddRange(texturesAssets);

        audioTask.Wait();

        assetBridge.Register(assets).Wait();

        //TODO : Data asset handling

        textureAssetInfos.Clear();
        audioAssetInfos.Clear(); 
        shaderAssets.Clear();
        fontAssets.Clear();
        dataAssets.Clear();
        dataHandlers.Clear();
    }
}

public abstract class Asset
{
    internal protected Asset(string identifier)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));

        Identifier = identifier.ToLower();
    }
    public string Identifier { get; }
}

public sealed class TextureAsset : Asset
{
    internal TextureAsset(string identifier, Color[] colors, Vector2 size) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));

        Colors = colors;
        Size = size;
    }

    public Color[] Colors { get; }
    public Vector2 Size { get; }
}

public sealed class AtlasAsset : Asset
{
    public readonly record struct Region
    {
        internal Region(string identifier, Rectangle rectangle)
        {
            Identifier = identifier.ToLower();
            Rectangle = rectangle;
        }

        public string Identifier { get; }
        public Rectangle Rectangle { get; }

        public override string ToString()
        {
            return $"Region [ Name: {Identifier}; Rectangle: {Rectangle} ]";
        }
    };

    internal AtlasAsset(string identifier, Color[] colors, Vector2 size, Region[] regions) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(colors, nameof(colors));
        ArgumentNullException.ThrowIfNull(regions, nameof(regions));

        Colors = colors;
        Size = size;
        Regions = regions;
    }

    public Color[] Colors { get; }
    public Vector2 Size { get; }
    public Region[] Regions { get; }

    public override string ToString()
    {
        return $"Atlas [ Size: {Size}; Regions: {Regions.Length} ]";
    }
}

public sealed class AudioAsset : Asset
{
    internal AudioAsset(string identifier, SoundEffect soundEffect) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(soundEffect, nameof(soundEffect));

        SoundEffect = soundEffect;
    }

    public SoundEffect SoundEffect { get; }
}

public sealed class XnbAsset : Asset
{
    public enum XnbType
    {
        Font,
        Shader
    }

    internal XnbAsset(string identifier, Stream stream) : base(identifier)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        if (stream is MemoryStream memoryStream)
        {
            Stream = memoryStream;
        }
        else
        {
            Stream = new MemoryStream();
            stream.CopyTo(Stream);
            Stream.Position = 0; 
        }

        //Getting XnbType
        byte[] header = new byte[512];
        int bytesRead = Stream.Read(header, 0, header.Length);
        Stream.Position = 0;

        int i = 10;

        while (i < bytesRead && (header[i] & 0x80) != 0) i++;
        i++;

        int strLen = 0, shift = 0;
        while (i < bytesRead)
        {
            byte b = header[i++];
            strLen |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
        }

        string typeName = System.Text.Encoding.UTF8.GetString(header, i, Math.Min(strLen, bytesRead - i));

        Type = typeName switch
        {
            var t when t.Contains("SpriteFontReader") => XnbAsset.XnbType.Font,
            var t when t.Contains("EffectReader") => XnbAsset.XnbType.Shader,
            _ => throw new InvalidDataException($"This file is not a valid XNB file or has an unrecognized format and only supports SpriteFonts or Effects")
        };
    }
    public XnbType Type { get; }
    public MemoryStream Stream { get; }
}

public sealed class DataAsset : Asset
{
    internal DataAsset(string id, string defType, string parent, bool isAbstract, JsonObject jsonObject) : base(id)
    {
        DefType = defType;
        Parent = parent;
        IsAbstract = isAbstract;
        JsonObject = jsonObject;
    }

    public string DefType { get; }
    public string Parent { get; }
    public bool IsAbstract { get; }
    public JsonObject JsonObject { get; }
}

// public sealed class PackageAsset : Asset
// {
//     public PackageAsset(string name, AssetIO.Info? assetInfo) : base(name, null)
//     {
//         throw new NotImplementedException(nameof(PackageAsset));
//     }
// }