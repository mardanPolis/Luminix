using System;
using System.Diagnostics;
using System.Threading;
using GameSrc;
using GameSrc.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SurvivalGame.GameSrc.Simulation;
using Verse.Bridges;
using Verse.Collections;
using Verse.Rendering;
using Verse.Resources;
using Verse.Simulation;
using Verse.Threading;

namespace Verse;
public class Main : Game
{
    private GraphicsDeviceManager graphics;

    private Thread mainThread = Thread.CurrentThread;
    private CoreThread coreThread = null;

    private const bool IS_DEBUG = true;
    private const uint TICK_RATE = 20;

    public const string VERSION = "1.0v";

    private DenseList<Test> testList = new DenseList<Test>();

    private long startBytes = 0;
    private long allocated = 0;
    private double frameTime;

    private Stopwatch stopwatch;
    private GameTime time;
    private Logger logger;

    public double FrameTime => frameTime;
    public double FrameDeltaTime => time != null ? time.ElapsedGameTime.TotalSeconds : 0;
    public double FPS => FrameDeltaTime > 0 ? 1000.0 / FrameDeltaTime : double.MaxValue;
    public long FrameRate => (long)(1.0 / TargetElapsedTime.TotalSeconds);
    public long FrameAllocationAmount => allocated;

    public Main()
    {
        stopwatch = Stopwatch.StartNew();
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    public GameTime GameTime => time;

    protected override void Initialize()
    {
        try
        {
            Thread.CurrentThread.Name = "RenderThread";

            logger = Logger.Get();
            logger.Log($"Started: Initialize");

            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0);

            coreThread = new CoreThread(TICK_RATE, IS_DEBUG);

            RendererRegistry.Register(typeof(GameDebugRenderer));
            RendererRegistry.Register(typeof(TestRenderer));
            RendererRegistry.Assign(testList, typeof(TestRenderer));
            RendererRegistry.FinalizeRegistry();

            //initialize thread bridges
            CoreToRender.Initialize();

            RenderingSystem.Initialize(this, graphics);
            ResourceManager.Initialize(Content.RootDirectory, Content);
            World.Initialize(coreThread);

            base.Initialize();

            if(RendererRegistry.TryGetDebugRenderer(typeof(GameDebugRenderer), out DebugRenderer renderer))
                ((GameDebugRenderer)renderer).Initialize(this, coreThread);

            RenderingSystem.InitializeRenderers(IS_DEBUG);

            logger.Log($"Successful: Initialize");
        }
        catch
        {
            Logger.Get().Log($"Exception happened during 'Initialize'");
            throw;
        }
    }

    protected override void LoadContent()
    {
        try
        {
            logger.Log($"Started: LoadContent");

            int assetLoadStart = stopwatch.Elapsed.Milliseconds;

            ResourceManager.LoadDefaultData();

            logger.Log($"Loaded Game assets in {stopwatch.Elapsed.Milliseconds - assetLoadStart}ms");

            // TODO: use this.Content to load your game content

            logger.Log($"Successful: LoadContent");
            base.LoadContent();
        }
        catch
        {
            Logger.Get().Log($"Exception happened during 'LoadContent'");
            throw;
        }
    }

    protected override void BeginRun()
    {
        try
        {
            logger.Log($"Started: Start");

            //TEST
            var testObject = new Test();
            testList.Add(testObject);

            new CameraMover();
            //END TEST

            foreach(var component in World.WorldComponents)
                component.Initialize();

            int threadCount = Process.GetCurrentProcess().Threads.Count;
            logger.Log($"Threads: {threadCount}");

            coreThread.Start();
            
            logger.Log($"Successful: Start");
            logger.Log($"Game fully initialized in {stopwatch.Elapsed.Milliseconds}ms");
        }
        catch
        {
            Logger.Get().Log($"Exception happened during 'BeginRun'");
            throw;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        startBytes = GC.GetAllocatedBytesForCurrentThread();
        stopwatch.Restart();

        KeyboardState keyboardState = Keyboard.GetState();

        if(keyboardState.IsKeyDown(Keys.C))
            Crash();

        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        RenderingSystem.DrawFrame(coreThread.PartialTick);
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

    public void Crash()
    {
        throw new Exception("Test Crash");
    }
}
