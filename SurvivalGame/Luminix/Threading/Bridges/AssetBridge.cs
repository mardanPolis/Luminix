
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Luminix.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Luminix.Threading.Bridges;

internal class AssetBridge
{
    private struct AssetTask(Asset asset, TaskCompletionSource tcs)
    {
        public Asset Asset { get; } = asset;
        public TaskCompletionSource Tcs { get; } = tcs;
    }
    private struct AssetGroupTask(IList<Asset> assets, TaskCompletionSource tcs)
    {
        public IList<Asset> Assets { get; } = assets;
        public TaskCompletionSource Tcs { get; } = tcs;
    }
    private ConcurrentQueue<AssetTask> tasks = new ConcurrentQueue<AssetTask>();
    private ConcurrentQueue<AssetGroupTask> groupTasks = new ConcurrentQueue<AssetGroupTask>();

    private List<TaskCompletionSource> taskCompletionSources = new List<TaskCompletionSource>();

    public void Register(Asset asset)
    {
        var task = new AssetTask(asset, null);
        tasks.Enqueue(task);
    }

    public void Register(IList<Asset> assets)
    {
        var task = new AssetGroupTask(assets, null);
        groupTasks.Enqueue(task);
    }

    // SYNC
    public void RegisterSync(Asset asset)
    {
        var task = new AssetTask(asset, new TaskCompletionSource());
        tasks.Enqueue(task);
        task.Tcs.Task.Wait();
    }

    public void RegisterSync(IList<Asset> assets)
    {
        var task = new AssetGroupTask(assets, new TaskCompletionSource());
        groupTasks.Enqueue(task);
        task.Tcs.Task.Wait();
    }
    // ASYNC
    public Task RegisterAsync(Asset asset)
    {
        var task = new AssetTask(asset, new TaskCompletionSource());
        tasks.Enqueue(task);
        return task.Tcs.Task;
    }
    public Task RegisterAsync(IList<Asset> assets)
    {
        var task = new AssetGroupTask(assets, new TaskCompletionSource());
        groupTasks.Enqueue(task);
        return task.Tcs.Task;
    }


    public void ConsumeAssets(Action<Asset> action, out List<TaskCompletionSource> _taskCompletionSources)
    {
        taskCompletionSources.Clear();

        while (tasks.TryDequeue(out AssetTask assetTask))
        {
            action(assetTask.Asset);
            if(assetTask.Tcs != null)
                taskCompletionSources.Add(assetTask.Tcs);
        }

        while (groupTasks.TryDequeue(out AssetGroupTask assetGroupTask))
        {
            for(int i = 0; i < assetGroupTask.Assets.Count; i++)
                action(assetGroupTask.Assets[i]);

            if(assetGroupTask.Tcs != null)
                taskCompletionSources.Add(assetGroupTask.Tcs);
        }

        _taskCompletionSources = taskCompletionSources;
    }
}