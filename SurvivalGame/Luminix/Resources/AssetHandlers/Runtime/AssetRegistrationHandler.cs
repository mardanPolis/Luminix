using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Luminix.IO;
using Luminix.Threading.Bridges;

namespace Luminix.Resources.AssetHandlers;

internal class AssetRegistrationHandler
{
    public AssetRegistrationHandler(AssetBridge assetBridge, ResourceBridge resourceBridges, ResourceManager resourceManager)
    {
        ArgumentNullException.ThrowIfNull(assetBridge, nameof(assetBridge));
        ArgumentNullException.ThrowIfNull(resourceManager, nameof(resourceManager));

        this.assetBridge = assetBridge;
        this.resourceManager = resourceManager;

        this.action = resourceManager.CreateResource;
        this.resourceBridge = resourceBridges;
    }

    private readonly AssetBridge assetBridge;
    private readonly Action<Asset> action;

    private readonly ResourceBridge resourceBridge;

    private readonly ResourceManager resourceManager;

    public void Run()
    {
        assetBridge.ConsumeAssets(action, out List<TaskCompletionSource> tsks);
        resourceBridge.Set(resourceManager.Resources);

        for(int i = 0; i < tsks.Count; i++)
            tsks[i].SetResult();
    }
}