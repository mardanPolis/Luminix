using System;
using System.Diagnostics;
using System.Threading;
using GameSrc;
using GameSrc.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Verse.Collections;
using Verse.Rendering;
using Verse.Resources;
using Verse.Simulation;
using Verse.Threads;

namespace Verse;
public class Main : Game
{
    private GraphicsDeviceManager graphics;

    private Camera camera;

    private Thread mainThread = Thread.CurrentThread;
    private CoreThread coreThread = null;

    private const bool IS_DEBUG = true;
    private const uint TICK_RATE = 20;

    private DenseList<Test> testList = new DenseList<Test>();

    public Main()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        try
        {
            Console.WriteLine($"Started: Initialize");
            // TODO: Add your initialization logic here
            RendererRegistry.Register(typeof(TestRenderer));
            RendererRegistry.Assign(testList, typeof(TestRenderer));
            RendererRegistry.FinalizeRegistry();

            ResourceManager.Initialize(Content.RootDirectory, Content);

            coreThread = new CoreThread(IS_DEBUG, TICK_RATE);
            World.Initialize(coreThread);

            base.Initialize();

            Console.WriteLine($"Successful: Initialize");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during Initialize: {ex}");
            Exit();
        }
    }

    protected override void LoadContent()
    {
        try
        {
            Console.WriteLine($"Started: LoadContent");
            camera = new Camera(GraphicsDevice.Viewport);

            RenderingSystem.Initialize(graphics, camera, IS_DEBUG);

            ResourceManager.LoadTexture("null_texture.png");
            ResourceManager.NewTextureRegion("null_texture", "null_texture.png", 0, 0, 32, 32);

            // TODO: use this.Content to load your game content

            Console.WriteLine($"Successful: LoadContent");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during LoadContent: {ex}");
            Exit();
        }
    }

    protected override void BeginRun()
    {
        try
        {
            Console.WriteLine($"Started: BeginRun");
            var testObject = new Test();
            testList.Add(testObject);

            foreach(var renderer in RendererRegistry.GetRenderers())
                renderer.Initialize();

            coreThread.Start();
            
            base.BeginRun();

            Console.WriteLine($"Successful: BeginRun");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during BeginRun: {ex}");
            Exit();
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (Keyboard.GetState().IsKeyDown(Keys.Q))
        {
            camera.OrthographicSize += 0.1f;
        }
        if (Keyboard.GetState().IsKeyDown(Keys.E))
            camera.OrthographicSize -= 0.1f;

        if (Keyboard.GetState().IsKeyDown(Keys.A))
            camera.PosX -= 0.1f;
        if (Keyboard.GetState().IsKeyDown(Keys.D))
            camera.PosX += 0.1f;

        if (Keyboard.GetState().IsKeyDown(Keys.W))
            camera.PosY += 0.1f;
        if (Keyboard.GetState().IsKeyDown(Keys.S))
            camera.PosY -= 0.1f;

        // TODO: Add your update logic here

        RenderingSystem.HotRender(gameTime.TotalGameTime.TotalSeconds / 1, gameTime.ElapsedGameTime.TotalSeconds);
        base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime)
    {
        RenderingSystem.DrawFrame(coreThread.PartialTick);
        base.Draw(gameTime);
    }
}
