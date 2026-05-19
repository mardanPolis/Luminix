using System;
using System.Collections.Generic;
using Luminix.Threading;
using Luminix.IO;
using Luminix.Threading.Bridges;
using Luminix.Collections;
using System.Linq;

namespace Luminix.Resources.AssetHandlers;

internal class AssetLoadingHandler
{
    private readonly WorkerThreadPool workerThreadPool;
    private readonly AssetIO assetIO;
    private readonly AssetBridge assetBridge;

    private readonly DenseList<string> loadedBootAssets;
    private readonly DenseList<string> loadedAssets;

    public AssetLoadingHandler(WorkerThreadPool pool, AssetIO io, AssetBridge assetBridge)
    {
        this.workerThreadPool   = pool;
        this.assetIO            = io;
        this.assetBridge        = assetBridge;
        this.loadedBootAssets   = new DenseList<string>();
        this.loadedAssets       = new DenseList<string>();
    }

    public void RunBootLoadingPhase()
    {
        List<string> paths = new List<string>();


    }
    public void RunLoadingPhase(Action<string, int, int> onLoad)
    {
        List<string> paths = new List<string>();
        
    }
    public HashSet<string> GetLoadedBootAssets() => loadedBootAssets.ToHashSet();
    public HashSet<string> GetLoadedAssets() => loadedAssets.ToHashSet();
}