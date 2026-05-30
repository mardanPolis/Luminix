using System.Collections.Generic;
using Luminix.Threading;
using Luminix.IO;
using Luminix.Collections;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Luminix.Resources.BuiltIn;
using System.Threading.Tasks;

namespace Luminix.Resources.AssetHandlers;

internal class AssetLoadingHandler
{
    private readonly WorkerThreadPool workerThreadPool;
    private readonly AssetIO assetIO;

    public AssetLoadingHandler(WorkerThreadPool pool, AssetIO io)
    {
        this.workerThreadPool = pool;
        this.assetIO          = io;
    }

    public DenseList<Asset> RunBootLoadingPhase()
    {
        TextureAsset nullTextureAsset = new ("null", BuiltInResources.NullTextureColor, new Vector2(32, 32));

        var assetFiles = assetIO.EnumerateAssets(boot: true).ToList();
        var assets = Load(assetFiles).GetAwaiter().GetResult();
        assets.Add(nullTextureAsset);
        return assets;
    }

    public DenseList<Asset> RunLoadingPhase()
    {
        var assetFiles = assetIO.EnumerateAssets(boot: false).ToList();
        return Load(assetFiles).GetAwaiter().GetResult();
    }

    private async Task<DenseList<Asset>> Load(List<AssetIO.AssetFile> assetFiles)
    {
        ConcurrentBag<Asset> loadedAssets = [];

        var tasks = assetFiles.Select(async assetFile =>
        {
            if (Settings.DebugAssets)
                Logger.Get().Log("Loading: " + assetFile.RelativePath);

            Asset asset = await assetIO.LoadAssetAsync(assetFile);
            loadedAssets.Add(asset);
        });

        await Task.WhenAll(tasks);

        var result = new DenseList<Asset>();

        while(loadedAssets.TryTake(out var item))
            result.Add(item);

        return result;
    }
}