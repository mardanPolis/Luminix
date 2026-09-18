using System;
using Luminix.Debug;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Luminix.Threading.Buffers;
using Luminix.UI;

namespace Luminix;

internal sealed class MainCore
{
    internal MainCore() {}

    private TickedThread coreThread;

    private TripleBufferBridge<RenderBuffer> renderBridge;
    private DoubleBufferBridge<RenderInfoBuffer> renderInfoBridge;
    private DoubleBufferBridge<InputBuffer> inputBridge;
    private ResourceBridge resourceBridge;
    private WindowBridge windowBridge;

    private World world;
    private World.Controller worldController;
    private Graphics graphics;
    private Graphics.Controller graphicsController;
    private Input input;
    private Input.Controller inputController;
    private GameObjectManager gameObjectManager;
    private GameObjectManager.Controller gameObjectManagerController;
    private UIManager uiManager;
    private UIManager.Controller uiManagerController;
    private ModuleManager moduleManager;
    private Defs defs;
    private Assets assets;
    private Resources resources;

    internal void Initialize()
    {
        try
        {
            renderBridge            = ThreadBridges.GetBridge<TripleBufferBridge<RenderBuffer>>(Application.RENDER_THREAD_NAME);
            renderInfoBridge        = ThreadBridges.GetBridge<DoubleBufferBridge<RenderInfoBuffer>>(Application.RENDER_THREAD_NAME);
            inputBridge             = ThreadBridges.GetBridge<DoubleBufferBridge<InputBuffer>>(Application.RENDER_THREAD_NAME);
            resourceBridge          = ThreadBridges.GetBridge<ResourceBridge>(Application.RENDER_THREAD_NAME);
            windowBridge            = ThreadBridges.GetBridge<WindowBridge>(Application.RENDER_THREAD_NAME);
            
            inputBridge.PopulateIfEmpty();

            coreThread = (TickedThread)Thread.CurrentThread;

            Thread.CurrentThread.IsBackground = true;

            TypeRegistry.Add(typeof(EntityType));
            TypeRegistry.Add(typeof(EntityType), "EntityType");

            TypeRegistry.SetDefaultSubType(typeof(IComponentType), typeof(ComponentType<>), true);

            Logger.NewLog(Logger.Log.Level.Info, $"Build-Vers: {Application.BuildVers}");
            Logger.NewLog(Logger.Log.Level.Info, $"Build-Date: {Application.BuildDate}");

            Logger.NewLog(Logger.Log.Level.Info, $"Started initialization");

            graphics    = new Graphics(out graphicsController);
            uiManager   = new UIManager(graphics, out uiManagerController);
            moduleManager     = new ModuleManager();
            gameObjectManager = new GameObjectManager(out gameObjectManagerController);
            input       = new Input(out inputController);
            resources   = new Resources();
            assets      = new Assets(resources);
            defs        = new Defs();
            world       = new World(graphics, out worldController);

            graphicsController.Start();

            defs.NewDefType<EntityType>(); // built in

            defs.AddDef(new EntityType("camera", null, (typeof(Camera), new Camera())));

            moduleManager.FindModules();
            moduleManager.InitializeModules();

            Logger.NewLog(Logger.Log.Level.Info, $"Found {moduleManager.ModuleCount} modules, {moduleManager.InitializedModuleCount} of them are initialized.");
            var logBuilder = new Logger.LogBuilder(Logger.Log.Level.Info);
            logBuilder.AppendLiteral("Found Modules: ");
            foreach(var module in moduleManager.Modules)
            {
                logBuilder.AppendLiteral("\n Module: ID - ");
                logBuilder.AppendFormatted(module.ID);
                logBuilder.AppendLiteral("; Name - ");
                logBuilder.AppendFormatted(module.Name);
            }
            logBuilder.End();

            logBuilder = new Logger.LogBuilder(Logger.Log.Level.Info);
            logBuilder.AppendLiteral("Initialized Modules: ");
            foreach(var module in moduleManager.InitializedModules)
            {
                logBuilder.AppendLiteral("\n Module: ID - ");
                logBuilder.AppendFormatted(module.ID);
                logBuilder.AppendLiteral("; Name - ");
                logBuilder.AppendFormatted(module.Name);
                logBuilder.AppendLiteral("; Has Assembly - ");
                logBuilder.AppendFormatted(module.HasAssembly());
                logBuilder.AppendLiteral("; Has Module Behavior - ");
                logBuilder.AppendFormatted(module.HasBehavior());
            }
            logBuilder.End();

            if(moduleManager.InitialModuleProblems.Length != 0)
            {
                logBuilder = new Logger.LogBuilder(Logger.Log.Level.Error);
                logBuilder.AppendLiteral("Failed to Initialize Modules: ");
                foreach(var problem in moduleManager.InitialModuleProblems)
                {
                    logBuilder.AppendLiteral("\n Problem - ");
                    logBuilder.AppendFormatted(problem);
                }
                logBuilder.End();
            }

            defs.FinalizeTypes();

            foreach(var module in moduleManager.InitializedModules)
            {
                var assetInfos = assets.GetAssetIO().EnumerateAssetInfos(module.GetModuleDirectory(), boot: true);
                foreach(var info in assetInfos)
                    assets.HandleAsset(info);
            }

            assets.FinalizeAssets(false, out bool hadAssets);

            Logger.NewLog(Logger.Log.Level.Info, "Loaded boot assets!");

            resources.Set();

            foreach(var module in moduleManager.InitializedModules)
            {
                var assetInfos = assets.GetAssetIO().EnumerateAssetInfos(module.GetModuleDirectory(), boot: false);
                foreach(var info in assetInfos)
                    assets.HandleAsset(info);
            }

            assets.FinalizeAssets(true, out hadAssets);

            Logger.NewLog(Logger.Log.Level.Info, "Loaded assets!");

            if(hadAssets)
                resources.Set();

            defs.PatchNodes();
            defs.InheritNodes();
            defs.DeserializeNodes();

            defs.FinalizeDefs();

            if(Application.IsDebug)
                gameObjectManager.Add(new DebugPanel());

            Logger.NewLog(Logger.Log.Level.Info, resources);

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

            uiManagerController.Clear();

            worldController.Tick();
            worldController.DestroyEntities();
            worldController.StartCreatedEntities();
            worldController.PrepareEntities();
            worldController.UpdateEntities();

            gameObjectManagerController.UpdateGameObjects();

            var mousePosition = input.MousePosition;
            inputController.IsMouseOverUI = uiManagerController.isMouseCollidingAt((int)mousePosition.X, (int)mousePosition.Y);

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

            worldController.RenderEntities();
            gameObjectManagerController.RenderGameObjects();
            uiManagerController.Render();

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