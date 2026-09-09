using System;
using System.Collections.Frozen;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Luminix.Threading.Buffers;
using Luminix.Threading.Commands;

namespace Luminix;

internal sealed class MainCore
{
    internal MainCore() {}

    private TickedThread coreThread;

    private TripleBufferBridge<RenderBuffer> renderBridge;
    private DoubleBufferBridge<RenderInfoBuffer> renderInfoBridge;
    private DoubleBufferBridge<InputBuffer> inputBridge;
    private ResourceBridge resourceBridge;
    private CommandBridge<WindowCommand> windowBridge;

    private World world;
    private World.Controller worldController;
    private Graphics graphics;
    private Graphics.Controller graphicsController;
    private Input input;
    private Input.Controller inputController;
    private Defs defs;
    private Assets assets;
    private Resources resources;

    internal void Initialize()
    {
        Logger.EchoToConsole = true;

        try
        {
            renderBridge            = ThreadBridges.GetBridge<TripleBufferBridge<RenderBuffer>>("RenderThread");
            renderInfoBridge        = ThreadBridges.GetBridge<DoubleBufferBridge<RenderInfoBuffer>>("RenderThread");
            inputBridge             = ThreadBridges.GetBridge<DoubleBufferBridge<InputBuffer>>("RenderThread");
            resourceBridge          = ThreadBridges.GetBridge<ResourceBridge>("RenderThread");
            windowBridge            = ThreadBridges.GetBridge<CommandBridge<WindowCommand>>("RenderThread");
            
            inputBridge.PopulateIfEmpty();

            coreThread = (TickedThread)Thread.CurrentThread;

            Thread.CurrentThread.IsBackground = true;

            TypeRegistry.Add(typeof(EntityType));
            TypeRegistry.Add(typeof(EntityType), "EntityType");

            TypeRegistry.SetDefaultSubType(typeof(IComponentType), typeof(ComponentType<>), true);

            Logger.NewLog(Logger.Log.Level.Info, $"Build-Vers: {Application.BuildVers}");
            Logger.NewLog(Logger.Log.Level.Info, $"Build-Date: {Application.BuildDate}");

            Logger.NewLog(Logger.Log.Level.Info, $"Started initialization");

            world           = new World(out worldController);
            graphics           = new Graphics(renderBridge, renderInfoBridge, out graphicsController);
            input              = new Input(inputBridge, out inputController);
            defs               = new Defs();
            resources          = new Resources(resourceBridge);
            assets             = new Assets(resources);

            defs.NewDefType<EntityType>(); // built in

            defs.FinalizeTypes();

            var bootAssetInfos = assets.GetAssetIO().EnumerateAssetInfos(boot: true);
            foreach(var info in bootAssetInfos)
                assets.HandleAsset(info);

            assets.FinalizeAssets(false, out bool hadAssets);

            Logger.NewLog(Logger.Log.Level.Info, "Loaded boot assets!");

            if(hadAssets)
                resources.Set();

            var nonBootAssetInfos = assets.GetAssetIO().EnumerateAssetInfos(boot: false);
            foreach(var info in nonBootAssetInfos)
                assets.HandleAsset(info);

            assets.FinalizeAssets(true, out hadAssets);

            Logger.NewLog(Logger.Log.Level.Info, "Loaded assets!");

            if(hadAssets)
                resources.Set();

            defs.PatchNodes();
            defs.InheritNodes();
            defs.DeserializeNodes();

            defs.FinalizeDefs();

            Thread.CurrentThread.IsBackground = false;
        }
        catch(Exception ex)
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Fatal Initialization Error: {ex}");
            Logger.Flush();
            throw;
        }
    }

    internal void Tick()
    {
        Update();
        Render();
    }

    internal void Update()
    {
        try
        {
            graphicsController.SwapBuffers();

            inputController.UpdateData();

            resources.TrySet();


            // var mousePosition = input.MousePosition;
            // inputController.IsMouseOverUI = uiManagerController.isMouseColliding((int)mousePosition.X, (int)mousePosition.Y);
        }
        catch(Exception ex)
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Fatal Update Error: {ex}");
            Logger.Flush();
            throw;
        }
    }
    
    internal void Render()
    {
        try
        {
            graphicsController.BeginRender();

            // entity rendering

            graphicsController.StopRender();
        }
        catch(Exception ex)
        {
            Logger.NewLog(Logger.Log.Level.Error, $"Fatal Render Error: {ex}");
            Logger.Flush();
            throw;
        }
    }

    internal void OnStop()
    {
        
    }
}