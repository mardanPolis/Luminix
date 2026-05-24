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

        {
            var assetLoader = new AssetLoadingHandler(workerThreadPool, assetIO);
            var atlasHandler = new TextureAssetAtlasHandler(workerThreadPool);

            //Loading Boot Assets
            Workspace.Current.SetSubState("Bootstrap");
            logger.Log(Workspace.Current.FullCurrentState);
            assetLoader.RunBootLoadingPhase();
            foreach(var texture in assetLoader.GetLoadedBootTextures())
            {
                string name = Path.GetFileNameWithoutExtension(texture.assetFile.RelativePath);
                CoreToRender.AssetBridge.RegisterTexture(name, texture.colors, texture.size);
                CoreToRender.AssetBridge.RegisterTextureRegion(name, name, Vector2.Zero, texture.size);
            }

            Workspace.Current.SetSubState("Receiving BootAssets");
            logger.Log(Workspace.Current.FullCurrentState);
            resources.WaitForResources(assetLoader.GetLoadedBootAssets(), RenderToCore.ResourceBridge);

            //Loading Assets
            Workspace.Current.SetSubState("Loading Assets");
            logger.Log(Workspace.Current.FullCurrentState);
            assetLoader.RunLoadingPhase();

            Workspace.Current.SetSubState("Generating Atlases");
            logger.Log(Workspace.Current.FullCurrentState);
            var results = atlasHandler.Run(assetLoader.GetLoadedTextures());
            foreach(var result in results)
            {
                CoreToRender.AssetBridge.RegisterTexture(result.atlasName, result.atlasResult.AtlasColors, result.atlasResult.AtlasSize);
                foreach(var region in result.atlasResult.TextureRegions)
                    CoreToRender.AssetBridge.RegisterTextureRegion(region.Name, result.atlasName, region.Rectangle);

                assetIO.SaveTexture(result.atlasName, result.atlasResult.AtlasColors, result.atlasResult.AtlasSize);
            }

            Workspace.Current.SetSubState("Receiving Assets");
            logger.Log(Workspace.Current.FullCurrentState);
            resources.WaitForResources(assetLoader.GetLoadedAssets(), RenderToCore.ResourceBridge);

            Workspace.Current.SetSubState("Renderers");
            logger.Log(Workspace.Current.FullCurrentState);
            //Renderers

            Workspace.Current.SetSubState("Types");
            logger.Log(Workspace.Current.FullCurrentState);
            //Types
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

        universeController.TickRate = TickRate;
        universeController.DeltaTime = UnsafeTickDeltaTime;
        universeController.TickCount = UnsafeTickCount;

        universe.World.Tick();
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
