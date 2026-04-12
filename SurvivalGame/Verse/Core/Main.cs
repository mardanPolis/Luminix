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
    private const bool IS_DEBUG = true;
    private const uint TICK_RATE = 20;
    private const uint TARGET_FPS = 144;
    public const string VERSION = "1.0v";

    public Main()
    {
        stopwatch = Stopwatch.StartNew();
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    private GraphicsDeviceManager graphics;

    private Thread mainThread = Thread.CurrentThread;
    private CoreThread coreThread = null;

    private DenseList<Test> testList = new DenseList<Test>();

    private long startBytes = 0;
    private long allocated = 0;
    private double frameTime;

    private Stopwatch stopwatch;
    private GameTime time;
    private Logger logger;

    public double FrameTime => frameTime;
    public double FrameDeltaTime => time != null ? time.ElapsedGameTime.TotalSeconds : 0;
    public double FPS => FrameDeltaTime > 0 ? 1.0 / FrameDeltaTime : double.MaxValue;
    public long FrameRate => (long)(1.0 / TargetElapsedTime.TotalSeconds);
    public long FrameAllocationAmount => allocated;

    public GameTime GameTime => time;

    private int mousePosition = 0; // X, Y
    private byte mouseButtonStates = 0; 
    private long mouseScrollPositions = 0; // vX, vY, hX, hY

    private long[] prevKeyboardStates = new long[4];
    private long[] currKeyboardStates = new long[4];

    protected override void Initialize()
    {
        try
        {
            startBytes = GC.GetAllocatedBytesForCurrentThread();
            
            Thread.CurrentThread.Name = "RenderThread";

            Logger.Initialize();

            logger = Logger.Get();
            logger.Log($"Started: Initialize");

            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / TARGET_FPS);

            mainThread.Priority = ThreadPriority.Highest;
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
            Logger.Get().Err($"Exception happened during 'Initialize'");
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
            Logger.Get().Err($"Exception happened during 'LoadContent'");
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

            long endBytes = GC.GetAllocatedBytesForCurrentThread();
            allocated = endBytes - startBytes;

            logger.Log($"Game fully initialized in {stopwatch.Elapsed.Milliseconds}ms");
            logger.Log($"Game allocated {allocated} bytes during initialization!");
        }
        catch
        {
            Logger.Get().Err($"Exception happened during 'BeginRun'");
            throw;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        stopwatch.Restart();
        startBytes = GC.GetAllocatedBytesForCurrentThread();

        HandleInput();

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

    private void HandleInput()
    {
        if(IsActive)
        {
            HandleKeyboardInput();
            HandleMouseInput();
            RenderToCore.InputBridge.Write(currKeyboardStates, mousePosition, mouseButtonStates);
        }
        else
        {
            Array.Clear(currKeyboardStates, 0, 4);

            var mouseState = Mouse.GetState();
            mousePosition = (mouseState.X << 16) | mouseState.Y; 
            RenderToCore.InputBridge.Write(currKeyboardStates, mousePosition, 0);
        }

        RenderToCore.WindowIsActive = IsActive;
    }

    private void HandleKeyboardInput()
    {
        Array.Copy(currKeyboardStates, prevKeyboardStates, 4);
        Array.Clear(currKeyboardStates, 0, 4);

        var keyboardState = Keyboard.GetState();
        
        if(keyboardState.GetPressedKeyCount() > 0)
        {
            for (int i = 0; i < 256; i++)
            {
                if (keyboardState.IsKeyDown((Keys)i))
                {
                    currKeyboardStates[i >> 6] |= 1L << (i & 63);
                }
            }
        }

        for (int i = 0; i < 4; i++)
        {
            long current = currKeyboardStates[i];
            long previous = prevKeyboardStates[i];
            
            long changed = current ^ previous;
            if (changed == 0) continue;

            long pressed = changed & current;
            long released = changed & previous;

            if (pressed != 0) DispatchEvents(i, pressed, true);
            if (released != 0) DispatchEvents(i, released, false);
        }

    }

    private void DispatchEvents(int chunkIndex, long mask, bool isDown)
    {
        int offset = chunkIndex << 6;
        for (int bit = 0; bit < 64; bit++)
        {
            if ((mask & (1L << bit)) != 0)
            {
                Keys key = (Keys)(offset | bit);
                if (isDown) RenderToCore.InputBridge.PressKeyEvent(key);
                else RenderToCore.InputBridge.ReleaseKeyEvent(key);
            }
        }
    }

    private void HandleMouseInput()
    {
        var mouseState = Mouse.GetState();

        //position
        short mouseX = (short)mouseState.X;
        short mouseY = (short)mouseState.Y;

        int currMousePosition = (mouseX << 16) | (int)mouseY;

        //mouseButtons

        byte currMouseButtonStates = 0;

        if (mouseState.LeftButton == ButtonState.Pressed) currMouseButtonStates = 1;
        if (mouseState.RightButton == ButtonState.Pressed) currMouseButtonStates |= 2;
        if (mouseState.MiddleButton == ButtonState.Pressed) currMouseButtonStates |= 4; 
        if (mouseState.XButton1 == ButtonState.Pressed) currMouseButtonStates |= 8;
        if (mouseState.XButton2 == ButtonState.Pressed) currMouseButtonStates |= 16;

        int changedButtons = currMouseButtonStates ^ mouseButtonStates;

        if (changedButtons != 0)
        {
            short X = (short)mouseState.X;
            short Y = (short)mouseState.Y;

            for (byte i = 0; i < 5; i++)
            {
                int mask = 1 << i;
                if ((changedButtons & mask) != 0)
                {
                    bool isDown = (currMouseButtonStates & mask) == 0;
                    DispatchMouseEvent(i, X, Y, isDown);
                }
            }
        }

        mousePosition = currMousePosition;
        mouseButtonStates = currMouseButtonStates;
    }

    private void DispatchMouseEvent(byte button, short X, short Y, bool down)
    {
        if(down)
            RenderToCore.InputBridge.ReleaseButtonEvent(button, X, Y);
        else
            RenderToCore.InputBridge.PressButtonEvent(button, X, Y);
    }
}
