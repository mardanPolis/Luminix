using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Verse.Rendering;

using var game = new SurvivalGame.Main();
game.Run();

namespace SurvivalGame
{
    public class Main : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

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
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

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
            RenderingSystem.BeginFrame(0f);

            // TODO: Add your update logic here

            RenderingSystem.HotRender();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            RenderingSystem.DrawFrame();

            base.Draw(gameTime);
        }
    }
}

