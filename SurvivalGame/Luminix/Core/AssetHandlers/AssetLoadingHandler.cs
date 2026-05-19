using System;
using System.Collections.Generic;
using Luminix.Threading;
using Luminix.IO;
using Luminix.Bridges

namespace Luminix.Core.AssetHandlers;

internal class AssetLoadingHandler
{
    private readonly WorkerThreadPool workerThreadPool;
    private readonly AssetIO assetIO;
    private readonly AssetBridge assetBridge;

    public AssetLoadingHandler(WorkerThreadPool pool, AssetIO io, AssetBridge assetBridge)
    {
        this.workerThreadPool   = pool;
        this.assetIO            = io;
        this.assetBridge        = assetBridge;
    }

    public void RunBootLoadingPhase() { }
    public void RunLoadingPhase(Action<string, int, int> onLoad) { }
    public HashSet<string> GetLoadedBootAssets() { return new HashSet<string>(); } //temp
    public HashSet<string> GetLoadedAssets() { return new HashSet<string>(); } //temp
}