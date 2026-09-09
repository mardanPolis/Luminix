using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Luminix.IO;
using Luminix.Threading.Bridges;
using Luminix.Threading.Commands;

namespace Luminix;

public sealed class Assets : Workspace.Service
{
    internal Assets(Resources resources)
    {
        this.assetIO = new AssetIO();
        this.resources = resources;

        this.textureAssetInfos = [];
        this.audioAssetInfos = [];

        this.textureAssets = [];
        this.atlasAssets = [];
        this.audioAssets = [];
        this.shaderAssets = [];
        this.fontAssets = [];

        this.dataAssets = [];
    }

    private readonly AssetIO assetIO;
    private readonly Resources resources;

    private readonly SortedDictionary<string, AssetIO.AssetInfo> textureAssetInfos;
    private readonly Dictionary<string, AssetIO.AssetInfo> audioAssetInfos;

    private readonly SortedDictionary<string, TextureAsset> textureAssets;
    private readonly Dictionary<string, AtlasAsset> atlasAssets;
    private readonly Dictionary<string, AudioAsset> audioAssets;
    private readonly Dictionary<string, XnbAsset> shaderAssets;
    private readonly Dictionary<string, XnbAsset> fontAssets;
    private readonly List<DataAsset> dataAssets;

    public AssetIO GetAssetIO() => assetIO;

    public void HandleAsset(in AssetIO.AssetInfo assetInfo)
    {
        if (assetIO.IsTextureAsset(assetInfo))
        {
            textureAssetInfos[assetInfo.RelativePath] = assetInfo;
        }
        else if(assetIO.IsAudioAsset(assetInfo))
        {
            audioAssetInfos[assetInfo.RelativePath] = assetInfo;
        }
        else
        {
            Asset asset = assetIO.LoadAsset(assetInfo);
            HandleAsset(asset);
        }
    }
    public void HandleAsset(Asset asset)
    {
        if(asset is TextureAsset texture)
        {
            textureAssets[texture.Identifier] = texture;
        }
        else if(asset is AtlasAsset atlas)
        {
            atlasAssets[atlas.Identifier] = atlas;
        }
        else if(asset is AudioAsset audio)
        {
            audioAssets[audio.Identifier] = audio;
        }
        else if(asset is XnbAsset xnb)
        {
            switch (xnb.Type)
            {
                case XnbAsset.XnbType.Font:
                    shaderAssets[xnb.Identifier] = xnb;
                break;
                case XnbAsset.XnbType.Shader:
                    fontAssets[xnb.Identifier] = xnb;
                break;
            }
        }
        else if(asset is DataAsset data)
        {
            dataAssets.Add(data);
        }
        else
        {
            throw new NotSupportedException($"Unable to resolve asset: {asset}.");
        }
    }

    public AtlasAsset[] CreateAtlasAssets(IReadOnlyList<TextureAsset> textureAssets)
    {
        var atlasGroups = new Dictionary<string, List<TextureAsset>>();

        //Grouping textures
        for (int i = 0; i < textureAssets.Count; i++)
        {
            var texture = textureAssets[i];

            if(texture == null)
            {
                Logger.NewLog(Logger.Log.Level.Error, "Unable to add texture to an atlas, reason: provided texture is null!");
                continue;
            }

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
        Parallel.ForAsync(0, atlasGroups.Count, async (index, ct) =>
        {
            var atlasMaker = new AtlasMaker();
            var atlasName = groupKeys[index];
            var textures  = groupValues[index];

            for(int i = 0; i < textures.Count; i++)
                atlasMaker.Add(textures[i]);

            var atlasResults = atlasMaker.Pack(atlasName);
            
            for(int i = 0; i < atlasResults.Length; i++)
                resultBag.Add(atlasResults[i]);
        }).GetAwaiter().GetResult();

        return resultBag.ToArray();
    }
    public AtlasAsset[] CreateAtlasAssets(IEnumerable<TextureAsset> textureAssets)
    {
        var atlasGroups = new Dictionary<string, List<TextureAsset>>();

        //Grouping textures
        foreach(var texture in textureAssets)
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
        Parallel.ForAsync(0, atlasGroups.Count, async (index, ct) =>
        {
            var atlasMaker = new AtlasMaker();
            var atlasName = groupKeys[index];
            var textures  = groupValues[index];

            for(int i = 0; i < textures.Count; i++)
                atlasMaker.Add(textures[i]);

            var atlasResults = atlasMaker.Pack(atlasName);
            
            for(int i = 0; i < atlasResults.Length; i++)
                resultBag.Add(atlasResults[i]);
        }).GetAwaiter().GetResult();

        return resultBag.ToArray();
    }

    internal void FinalizeAssets(bool makeAtlases, out bool hadAssets)
    {
        var defs = Workspace.Current.GetService<Defs>();
        var assets = new List<Asset>();

        AssetIO.AssetInfo[] textureInfos = textureAssetInfos.Values.ToArray();
        TextureAsset[] textureResults = new TextureAsset[textureInfos.Length];
        var textureTask = Parallel.ForAsync(0, textureInfos.Length, async (i, ct) =>
        {
            try
            {
                textureResults[i] = await assetIO.LoadTextureAssetAsync(textureInfos[i]);
            }
            catch (Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Fatal Error: {ex}");
                throw;
            }
        });

        AssetIO.AssetInfo[] audioInfos = audioAssetInfos.Values.ToArray();
        AudioAsset[] audioResults = new AudioAsset[audioInfos.Length];
        var audioTask = Parallel.ForAsync(0, audioInfos.Length, async (i, ct) =>
        {
            try
            {
                audioResults[i] = await assetIO.LoadAudioAssetAsync(audioInfos[i]);
            }
            catch (Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Fatal Error: {ex}");
                throw;
            }
        });

        assets.AddRange(textureAssets.Values);
        assets.AddRange(atlasAssets.Values);
        assets.AddRange(audioAssets.Values);
        assets.AddRange(shaderAssets.Values);
        assets.AddRange(fontAssets.Values);

        var pending = new List<Task> { audioTask, textureTask };
        Task.WhenAll(pending).GetAwaiter().GetResult();

        assets.AddRange(audioResults);
        assets.AddRange(makeAtlases ? CreateAtlasAssets(textureResults) : textureResults);

        for(int i = 0; i < dataAssets.Count; i++)
        {
            defs.ParseDataAsset(dataAssets[i]);
        }

        var assetTasks = new List<Task>();
        foreach(var asset in assets)
        {
            assetTasks.Add(resources.CreateResourceFromAsset(asset));
        }

        Task.WhenAll(assetTasks).GetAwaiter().GetResult();

        textureAssetInfos.Clear();
        audioAssetInfos.Clear(); 

        textureAssets.Clear();
        audioAssets.Clear();
        atlasAssets.Clear();
        shaderAssets.Clear();
        fontAssets.Clear();
        dataAssets.Clear();

        hadAssets = assetTasks.Count > 0;
    }
}