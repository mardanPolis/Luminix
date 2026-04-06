using System;
using System.Diagnostics;
using System.Threading;
using GameSrc;
using GameSrc.Rendering;
using Microsoft.Xna.Framework;
using SurvivalGame.GameSrc.Simulation;
using Verse.Bridges;
using Verse.Collections;
using Verse.Rendering;
using Verse.Resources;
using Verse.Simulation;
using Verse.Threads;

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
            Console.WriteLine($"Started: Initialize");

            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0);

            coreThread = new CoreThread(IS_DEBUG, TICK_RATE);

            RendererRegistry.Register(typeof(GameDebugRenderer));
            RendererRegistry.Register(typeof(TestRenderer));
            RendererRegistry.Assign(testList, typeof(TestRenderer));
            RendererRegistry.FinalizeRegistry();

            //initialize thread bridges
            CoreToRender.Initialize();

            RenderingSystem.Initialize(graphics);
            ResourceManager.Initialize(Content.RootDirectory, Content);
            World.Initialize(coreThread);

            base.Initialize();

            if(RendererRegistry.TryGetDebugRenderer(typeof(GameDebugRenderer), out DebugRenderer renderer))
                ((GameDebugRenderer)renderer).Initialize(this, coreThread);

            RenderingSystem.InitializeRenderers(IS_DEBUG);

            Console.WriteLine($"Successful: Initialize");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during Initialize: {ex}");
            throw;
        }
    }

    protected override void LoadContent()
    {
        try
        {
            Console.WriteLine($"Started: LoadContent");

            int assetLoadStart = stopwatch.Elapsed.Milliseconds;

            ResourceManager.LoadDefaultData();

            Console.WriteLine($"Loaded Game assets in {stopwatch.Elapsed.Milliseconds - assetLoadStart}ms");

            // TODO: use this.Content to load your game content

            Console.WriteLine($"Successful: LoadContent");
            base.LoadContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during LoadContent: {ex}");
            throw;
        }
    }

    protected override void BeginRun()
    {
        try
        {
            Console.WriteLine($"Started: Start");

            //TEST
            var testObject = new Test();
            testList.Add(testObject);

            new CameraMover();
            //END TEST

            foreach(var component in World.WorldComponents)
                component.Initialize();

            coreThread.Start();
            
            Console.WriteLine($"Successful: Start");
            Console.WriteLine($"Game fully initialized in {stopwatch.Elapsed.Milliseconds}ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during BeginRun: {ex}");
            throw;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        long endBytes = GC.GetAllocatedBytesForCurrentThread();
        allocated = endBytes - startBytes;
        startBytes = GC.GetAllocatedBytesForCurrentThread();
        stopwatch.Restart();

        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        RenderingSystem.DrawFrame(coreThread.PartialTick);
        base.Draw(gameTime);

        frameTime = stopwatch.Elapsed.TotalNanoseconds;
        time = gameTime;
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        coreThread.Stop();
        //save current world
    }
}
