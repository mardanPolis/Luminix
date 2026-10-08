using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Luminix.IO;

namespace Luminix;

public sealed class Assets : Workspace.Service
{
    internal Assets(Resources resources)
    {
        this.assetIO = new AssetIO();
        this.resources = resources;

        this.textureAssets = [];
        this.audioAssets = [];
        this.shaderAssets = [];
        this.fontAssets = [];

        this.dataAssets = [];
    }

    private readonly AssetIO assetIO;
    private readonly Resources resources;

    private readonly SortedDictionary<string, (TextureAsset, AssetIO.AssetInfo)> textureAssets;
    private readonly Dictionary<string, (AudioAsset, AssetIO.AssetInfo)> audioAssets;
    private readonly Dictionary<string, XnbAsset> shaderAssets;
    private readonly Dictionary<string, XnbAsset> fontAssets;
    private readonly List<DataAsset> dataAssets;

    public void HandleAsset(in AssetIO.AssetInfo assetInfo)
    {
        if (assetIO.IsTextureAsset(assetInfo))
        {
            textureAssets[assetInfo.RelativePath.ToLower()] = (null, assetInfo);
        }
        else if(assetIO.IsAudioAsset(assetInfo))
        {
            audioAssets[assetInfo.RelativePath.ToLower()] = (null, assetInfo);
        }
        else // XNB assets are loaded instantly because I have to determine its type and Data assets are also loaded because they do not follow the standart .
        {
            Asset asset = assetIO.LoadAsset(assetInfo);
            HandleAsset(asset);
        }
    }
    public void HandleAsset(Asset asset)
    {
        if(asset is TextureAsset texture)
        {
            textureAssets[texture.Identifier] = (texture, default);
        }
        else if(asset is AudioAsset audio)
        {
            audioAssets[audio.Identifier] = (audio, default);
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
            throw new NotSupportedException($"Unable to handle asset, reason: Unsupported asset type '{asset}'");
        }
    }

    private AtlasAsset[] CreateAtlasAssets(IReadOnlyList<TextureAsset> textureAssets)
    {
        var atlasGroups = new Dictionary<string, List<TextureAsset>>();

        //Grouping textures
        for (int i = 0; i < textureAssets.Count; i++)
        {
            var texture = textureAssets[i];

            if(texture == null)
                continue;

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

    public AssetIO GetAssetIO() => assetIO;

    internal void FinalizeAssets(bool makeAtlases, out bool hadAssets)
    {
        var defs = Workspace.Current.GetService<Defs>();
        var assets = new List<Asset>();

        (TextureAsset, AssetIO.AssetInfo)[] textures = textureAssets.Values.ToArray();
        TextureAsset[] textureResults = new TextureAsset[textures.Length];
        var textureTask = Parallel.ForAsync(0, textures.Length, async (i, ct) =>
        {
            try
            {
                if(textures[i].Item1 != null)
                    textureResults[i] = textures[i].Item1;
                else
                    textureResults[i] = assetIO.LoadTextureAsset(textures[i].Item2);
            }
            catch (Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Fatal Error: {ex}");
                throw;
            }
        });

        (AudioAsset, AssetIO.AssetInfo)[] audios = audioAssets.Values.ToArray();
        AudioAsset[] audioResults = new AudioAsset[audios.Length];
        var audioTask = Parallel.ForAsync(0, audios.Length, async (i, ct) =>
        {
            try
            {
                if(audios[i].Item1 != null)
                    audioResults[i] = audios[i].Item1;
                else
                    audioResults[i] = assetIO.LoadAudioAsset(audios[i].Item2);
            }
            catch (Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Fatal Error: {ex}");
                throw;
            }
        });

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

        foreach(var asset in assets)
        {
            try
            {
                resources.CreateResource(asset);
            }
            catch(Exception ex)
            {
                Logger.NewLog(Logger.Log.Level.Error, $"Failed to create resource, reason: {ex}");
            }
        }

        Task.WhenAll(assetTasks).GetAwaiter().GetResult();

        textureAssets.Clear();
        audioAssets.Clear();
        shaderAssets.Clear();
        fontAssets.Clear();
        dataAssets.Clear();

        hadAssets = assets.Count > 0;
    }
}