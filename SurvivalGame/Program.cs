using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Verse.Rendering;
using Verse.Threads;

using var game = new SurvivalGame.Main();
game.Run();

namespace SurvivalGame
{
    public class Main : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private Thread mainThread = Thread.CurrentThread;
        private CoreThread coreThread = null;

        uint totalFrameCount = 0;
        const uint TICK_RATE = 20;

        public Main()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            RendererRegistry.FinalizeRegistry();

            RenderingSystem.Initialize(graphics);

            coreThread = new CoreThread(true, TICK_RATE);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content

            RenderingSystem.SetSpriteBatch(spriteBatch);
        }

        protected override void BeginRun()
        {
            Console.WriteLine($"TickRate:{coreThread.TickRate}");
            Console.WriteLine($"tickIntervalInTicks:{TimeSpan.TicksPerSecond / coreThread.TickRate}\n");

            coreThread.Start();
            base.BeginRun();
        }
        
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            totalFrameCount++;
    
            Console.WriteLine($"FRAME : {totalFrameCount} \nTickTime:{coreThread.TickTimeInUS} \nAvgTickTime:{coreThread.AvgTickTimeInUS} \nTickCount:{coreThread.TickCount} \nPartialTick:{coreThread.PartialTick}");

            RenderingSystem.HotRender(gameTime.TotalGameTime.TotalSeconds / totalFrameCount, gameTime.ElapsedGameTime.TotalSeconds);
            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            RenderingSystem.DrawFrame(coreThread.PartialTick);

            base.Draw(gameTime);
        }
    }
}

