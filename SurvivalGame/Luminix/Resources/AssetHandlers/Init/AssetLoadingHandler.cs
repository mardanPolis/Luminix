using System.Collections.Generic;
using Luminix.Threading;
using Luminix.IO;
using Luminix.Collections;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Luminix.Resources.BuiltIn;
using System.Threading.Tasks;
using System;

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

    private Dictionary<string, AssetIO.AssetFile> assets = new Dictionary<string, AssetIO.AssetFile>();

    public DenseList<Asset> Run(bool boot)
    {
        var assetFiles = assetIO.EnumerateAssets(boot: boot).ToList();
        return Load(assetFiles);
    }
    private DenseList<Asset> Load(List<AssetIO.AssetFile> assetFiles)
    {
        List<AssetIO.AssetFile> validAssetFiles = new List<AssetIO.AssetFile>(assetFiles.Count);

        var logger = Logger.Get();
        for(int i = assetFiles.Count - 1; i >= 0; i--)
        {
            var assetFile = assetFiles[i];
            if(assets.TryAdd(assetFile.Name, assetFile))
                validAssetFiles.Add(assetFile);
            else
                logger.Wrn($"Duplicate assets with name '{assetFile.Name}' at locations: '{assetFile.RelativePath}' '{assets[assetFile.Name].RelativePath}'");
        }

        logger.Log("Loading");

        ConcurrentBag<Asset> loadedAssets = [];

        workerThreadPool.ParallelFor(validAssetFiles, assetFile =>
        {
            if(Settings.DebugInitialization && Settings.DebugAssets)
                Logger.Get().Log($"Loading asset at path: {assetFile.FullPath}");

            try
            {
                assetIO.LoadAsset(assetFile, out Asset asset);
                loadedAssets.Add(asset);
            }
            catch(Exception ex)
            {
                Logger.Get().Log($"Failed to load asset '{assetFile.FullPath}', reason: {ex}");
            }
        });

        var result = new DenseList<Asset>();

        while(loadedAssets.TryTake(out var item))
            result.Add(item);

        return result;
    }
}