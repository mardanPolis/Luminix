using System;
using System.Diagnostics;
using System.Threading;
using Luminix.Threading.Bridges;

namespace Luminix.Threading;

public sealed class CoreThread : TickedThread
{
    internal CoreThread(RenderBridge renderBridge, AssetBridge assetBridge, InputBridge inputBridge, ResourceBridge resourceBridge) : base(60)
    {   
        this.Name = "CoreThread";
        this.IsBackground = false;

        this.renderBridge = renderBridge;
        this.inputBridge = inputBridge;
        this.resourceBridge = resourceBridge;
        this.assetBridge = assetBridge;
    }

    private double tickUpdateTime = 0;
    private double tickRenderTime = 0;

    public double TickUpdateTime => Volatile.Read(ref tickUpdateTime);
    public double TickRenderTime => Volatile.Read(ref tickRenderTime);


    private RenderBridge renderBridge;
    private InputBridge inputBridge;
    private ResourceBridge resourceBridge;
    private AssetBridge assetBridge;

    private Universe universe;
    private Universe.Controller universeController;
    private Graphics graphics;
    private Graphics.Controller graphicsController;
    private Input input;
    private Input.Controller inputController;
    private Defs defs;
    private Assets assets;
    private Resources resources;
    
    protected sealed override void Initialize()
    {
        IsBackground = true;

        logger.NewLog($"Started initialization");

        universe           = new Universe(out universeController);
        graphics           = new Graphics(renderBridge, out graphicsController);
        input              = new Input(inputBridge, out inputController);
        defs               = new Defs();
        assets             = new Assets(assetBridge);
        resources          = new Resources();

        //TODO : Add entityType registration.

        var stopwatch = Stopwatch.StartNew();

        defs.NewDefType<EntityType>(); // built in

        defs.FinalizeTypes();

        var bootAssetInfos = assets.GetAssetIO().EnumerateAssetInfos(boot: true);
        foreach(var info in bootAssetInfos)
            assets.HandleAsset(info);

        assets.FinalizeAssets(false);

        var nonBootAssetInfos = assets.GetAssetIO().EnumerateAssetInfos(boot: false);
        foreach(var info in nonBootAssetInfos)
            assets.HandleAsset(info);

        assets.FinalizeAssets(true);

        defs.PatchNodes();
        defs.InheritNodes();
        defs.DeserializeNodes();

        defs.FinalizeDefs();

        long before = GC.GetTotalMemory(false);
        GC.Collect();
        long after = GC.GetTotalMemory(true);

        long cleared = before - after;
        logger.NewLog($"GC cleared: {cleared / 1024.0:F2} KB ({cleared:N0} bytes)");
        logger.NewLog($"Initialized in {stopwatch.Elapsed.Milliseconds}ms");


        //TODO : debug actions

        // debugKeyActions[0] = () => Settings.DebugInfo = !Settings.DebugInfo;                 //F1
        // debugKeyActions[1] = () => Settings.DebugConsole = !Settings.DebugConsole;           //F2
        // debugKeyActions[2] = () => Settings.DebugInspector = !Settings.DebugInspector;       //F3
        // debugKeyActions[3] = () => Settings.DebugGizmosPanel = !Settings.DebugGizmosPanel;   //F4
        // debugKeyActions[4] = () => Settings.DebugEventPanel = !Settings.DebugEventPanel;     //F5
        // debugKeyActions[5] = () => { logger.Wrn("Fullscreen not implemented!"); };           //F11
        // debugKeyActions[6] = () => { logger.Wrn("Screenshot not implemented!"); };           //F12

        IsBackground = false;
    }

    protected override void DebugInitialize()
    {

    }

    protected override void Tick()
    {
        Update();
        Render();
    }

    private void Update()
    {
        logger.Refresh();

        graphicsController.SwapBuffers();

        inputController.UpdateData();

        resources.Set(resourceBridge.Get());

        universeController.TickRate = TickRate;
        universeController.TickTime = TickTime;
        universeController.TickCount = TickCount;
        universeController.TickDeltaTime = TickDeltaTime;
        universeController.TickAllocated = TickAllocated;

        // var mousePosition = input.MousePosition;
        // inputController.IsMouseOverUI = uiManagerController.isMouseColliding((int)mousePosition.X, (int)mousePosition.Y);
    }
    
    private void Render()
    {
        graphicsController.BeginRender();

        // entity rendering

        graphicsController.StopRender();
    }
}
