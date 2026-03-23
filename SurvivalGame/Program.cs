using System;
using System.Diagnostics;
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
            RenderingSystem.Initialize(graphics);

            coreThread = new CoreThread(TICK_RATE);
            coreThread.Start();

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
            long usedBytes = GC.GetTotalMemory(forceFullCollection: false);
            Console.WriteLine($"Managed memory: {usedBytes / 1024.0 / 1024.0:F2} MB");
            base.BeginRun();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            totalFrameCount++;

            // TODO: Add your update logic here

            RenderingSystem.HotRender(gameTime.TotalGameTime.Seconds / totalFrameCount, gameTime.ElapsedGameTime.Seconds);
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

