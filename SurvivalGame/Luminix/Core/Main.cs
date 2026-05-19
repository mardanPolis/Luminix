using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Game;
using Luminix.Collections;
using Luminix.Rendering;
using Luminix.Resources;
using Luminix.Threading;
using Luminix.Threading.Bridges;
using Luminix.Workspaces;
using Luminix.Core.InputHandlers;

namespace Luminix;
public class Main : Microsoft.Xna.Framework.Game
{
    public const string VERSION = "1.0v";

    public Main()
    {
        Content = null;
        IsMouseVisible = true;
    }

    private Thread mainThread = Thread.CurrentThread;
    private CoreThread coreThread = null;

    private long startBytes = 0;
    private long allocated = 0;
    private double frameTime;

    private Stopwatch stopwatch;
    private GameTime time;
    private Logger logger;

    private Workspace workspace;
    private ResourceProvider resources;
    private RenderingSystem renderingSystem;

    private ResourceManager resourceManager;
    private GraphicsDeviceManager graphics;

    private InputHandler inputHandler;

    public double FrameTime => frameTime;
    public double FrameDeltaTime => time != null ? time.ElapsedGameTime.TotalSeconds : 0;
    public double FPS => FrameDeltaTime > 0 ? 1.0 / FrameDeltaTime : double.MaxValue;
    public long FrameRate => (long)(1.0 / TargetElapsedTime.TotalSeconds);
    public long FrameAllocationAmount => allocated;

    public GameTime GameTime => time;

    public new ContentManager Content
    {
        init => base.Content = value;
    }

    public ResourceProvider Resources => resources;
    internal ResourceManager ResourceManager => resourceManager;
    internal GraphicsDeviceManager GraphicsDeviceManager => graphics;

    protected override void Initialize()
    {
        stopwatch = Stopwatch.StartNew();
        startBytes = GC.GetAllocatedBytesForCurrentThread();
        
        logger = Logger.Initialize();
        logger.Log($"Started initialization");

        try
        {
            //workspace
            workspace = Workspace.Create();

            //services
            graphics = new GraphicsDeviceManager(this);
            resourceManager = new ResourceManager(this);

            //threads
            coreThread = new CoreThread();

            //systems
            renderingSystem = new RenderingSystem(this);

            //handlers
            inputHandler = new InputHandler();

            //initialize thread bridges
            CoreToRender.Initialize();
            RenderToCore.Initialize();

            //threads
            mainThread.Priority = ThreadPriority.AboveNormal;
            mainThread.Name = "RenderThread";

            coreThread.Priority = ThreadPriority.AboveNormal;

            //base initialization
            base.Initialize();

            //Renderer initialization   
            renderingSystem.Initialize();

            TargetElapsedTime = TimeSpan.FromSeconds(1d / 30d);
            IsFixedTimeStep = true;

            //starting threads
            coreThread.Start();

        }
        catch
        {
            Logger.Get().Err($"Exception happened during initialization");
            throw;
        }

        logger.Log($"Successful initialization");
        
        long endBytes = GC.GetAllocatedBytesForCurrentThread();
        allocated = endBytes - startBytes;

        logger.Log($"Game initialized in {stopwatch.Elapsed.Milliseconds}ms");
        logger.Log($"Game allocated {allocated} bytes during initialization!");

        workspace.SetState(Workspace.State.Running);
    }
    protected override void Update(GameTime gameTime)
    {
        stopwatch.Restart();
        startBytes = GC.GetAllocatedBytesForCurrentThread();

        inputHandler.Update(IsActive);
        RenderToCore.WindowIsActive = IsActive;

        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        renderingSystem.DrawFrame(coreThread.PartialTick);
        base.Draw(gameTime);

        frameTime = stopwatch.Elapsed.TotalNanoseconds;
        time = gameTime;
        long endBytes = GC.GetAllocatedBytesForCurrentThread();
        allocated = endBytes - startBytes;
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        coreThread.Stop();
        //save current world
    }
}
