using System;
using System.Diagnostics;
using System.Threading;
using Luminix.Simulation;
using Luminix.Workspaces;
using Luminix.IO;
using Luminix.Resources;
using Luminix.Resources.AssetHandlers;
using Luminix.Threading.Bridges;
using System.IO;
using Microsoft.Xna.Framework;
using Luminix.AssetHandlers;
using Luminix.Resources.BuiltIn;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Luminix.Threading;

public sealed class CoreThread : TickedThread
{
    internal CoreThread(int workerThreadCount) : base(20)
    {   

        this.Name = "CoreThread";
        this.IsBackground = false;
        this.workerThreadCount = workerThreadCount;
    }

    private double tickUpdateTime = 0;
    private double tickRenderTime = 0;

    public double TickUpdateTime => Volatile.Read(ref tickUpdateTime);
    public double TickRenderTime => Volatile.Read(ref tickRenderTime);

    private int workerThreadCount;

    private Logger logger;

    private Universe universe;
    private ResourceProvider resources;
    private TypeRegistry typeRegistry;
    private Universe.Controller universeController;
    private Input input;
    private WorkerThreadPool workerThreadPool;
    private AssetIO assetIO;

    protected sealed override void Initialize()
    {
        logger = Logger.Get();

        IsBackground = true;

        Workspace.Create();
        typeRegistry = new TypeRegistry();
        resources    = new ResourceProvider();
        universe     = new Universe(out universeController);
        input        = new Input();
        workerThreadPool = new WorkerThreadPool(workerThreadCount);
        assetIO          = new AssetIO();

        workerThreadPool.Initialize();

        {
            var assetLoadingHandler = new AssetLoadingHandler(workerThreadPool, assetIO);
            var textureAtlasHandler = new TextureAssetAtlasHandler(workerThreadPool);

            {
                List<Task> tasks = new List<Task>();

                //Loading Boot Assets
                Workspace.Current.SetSubState("bootstrap");

                Task nullTextureTask = CoreToRender.AssetBridge.RegisterTextureWithRegionAsync("null", BuiltInTextures.NullTextureColor, new Vector2(32, 32));
                tasks.Add(nullTextureTask);

                //TODO : make other asset handling.

                var loadedAssets = assetLoadingHandler.RunBootLoadingPhase();
                foreach(var texture in loadedAssets.Textures)
                {
                    string name = Path.GetFileNameWithoutExtension(texture.assetFile.RelativePath);
                    Task task = CoreToRender.AssetBridge.RegisterTextureWithRegionAsync(name, texture.colors, texture.size);
                    tasks.Add(task);
                }

                Workspace.Current.SetSubState("receiving boot assets");
                Task.WhenAll(tasks).Wait();
                resources.Update(RenderToCore.ResourceBridge);

                GC.Collect();
            }
            {
                List<Task> tasks = new List<Task>();

                //Loading Assets
                Workspace.Current.SetSubState("loading assets");

                var loadedAssets = assetLoadingHandler.RunLoadingPhase();

                Workspace.Current.SetSubState("generating atlases");
                var results = textureAtlasHandler.Run(loadedAssets.Textures);
                foreach(var result in results)
                {
                    Task textureTask = CoreToRender.AssetBridge.RegisterTextureAsync(result.atlasName, result.atlasResult.AtlasColors, result.atlasResult.AtlasSize);
                    tasks.Add(textureTask);
                    foreach(var region in result.atlasResult.TextureRegions)
                    {
                        Task regionTask = CoreToRender.AssetBridge.RegisterTextureRegionAsync(region.Name, result.atlasName, region.Rectangle);
                        tasks.Add(regionTask);
                    }

                    assetIO.SaveTexture(result.atlasName, result.atlasResult.AtlasColors, result.atlasResult.AtlasSize);
                }

                Workspace.Current.SetSubState("receiving assets");
                Task.WhenAll(tasks).Wait();
                resources.Update(RenderToCore.ResourceBridge);

                GC.Collect();
            }

            //TODO : Complete the initialization.

            //Renderers
            Workspace.Current.SetSubState("renderers");

            //Types
            Workspace.Current.SetSubState("types");
        }

        IsBackground = false;

        GC.Collect();
    }

    protected sealed override void OnTick()
    {
        Tick();
        Render();
    }

    protected sealed override void OnDebugTick()
    {
        Tick();
        long updateTime = CurrentElapsedTicks;
        Render();
        long renderTime = CurrentElapsedTicks;
        
        //avg tick time
        TimeSpan elapsedUpdateTime = Stopwatch.GetElapsedTime(0, updateTime);
        TimeSpan elapsedRenderTime = Stopwatch.GetElapsedTime(updateTime, renderTime);

        Interlocked.Exchange(ref tickUpdateTime, elapsedUpdateTime.TotalMicroseconds);
        Interlocked.Exchange(ref tickRenderTime, elapsedRenderTime.TotalMicroseconds);
    }

    private void Tick()
    {
        Bridges.CoreToRender.RenderBridge.Swap();

        input.ReadFrom(Bridges.RenderToCore.InputBridge);
 
        resources.Update(RenderToCore.ResourceBridge);

        universeController.TickRate = TickRate;
        universeController.DeltaTime = UnsafeTickDeltaTime;
        universeController.TickCount = UnsafeTickCount;

        universe.World?.Tick();
    }
    
    private void Render()
    {
        Optimize(TickIntervalInTicks, CurrentElapsedTicks);
    }

    private void Optimize(long maxTime, long time)
    {
        //if(maxTime * 0.6 > time) currWriteStack.Optimize();
    }
}
