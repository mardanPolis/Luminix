using System;
using System.Diagnostics;
using System.Threading;
using GameSrc;
using GameSrc.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SurvivalGame.GameSrc.Simulation;
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

            //set fps to 500
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 144.0);

            coreThread = new CoreThread(IS_DEBUG, TICK_RATE);

            RendererRegistry.Register(typeof(TestRenderer));
            RendererRegistry.Assign(testList, typeof(TestRenderer));
            RendererRegistry.FinalizeRegistry();

            ResourceManager.Initialize(Content.RootDirectory, Content);

            World.Initialize(coreThread);

            base.Initialize();

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

            RenderingSystem.Initialize(graphics, IS_DEBUG);

            ResourceManager.LoadTexture("null_texture.png");
            ResourceManager.NewTextureRegion("null_texture", "null_texture.png", 0, 0, 32, 32);

            // TODO: use this.Content to load your game content

            Console.WriteLine($"Successful: LoadContent");
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
            Console.WriteLine($"Started: BeginRun");
            //TEST
            var testObject = new Test();
            testList.Add(testObject);

            new CameraMover();
            //END TEST

            foreach(var renderer in RendererRegistry.GetRenderers())
                renderer.Initialize();

            foreach(var component in World.WorldComponents)
                component.Initialize();

            coreThread.Start();
            
            base.BeginRun();

            Console.WriteLine($"Successful: BeginRun");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during BeginRun: {ex}");
            throw;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

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
