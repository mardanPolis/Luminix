using System;
using System.Reflection;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SurvivalGame.Core;
using System.Collections.Generic;
using System.Linq;
using SurvivalGame.Core.EntitySystem;

namespace SurvivalGame;

public class SurvivalGame : Game
{
    public static readonly string GameId = "survivalgame";
    public static SurvivalGame Instance { private set; get; }
    public static readonly Assembly[] Assemblies = AppDomain.CurrentDomain.GetAssemblies();
    internal static Logger Logger;

    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;

    public SurvivalGame()
    {
        graphics = new GraphicsDeviceManager(this);
        Logger = new Logger();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        //Register Entity Tags

        //Register Entities

        Entities.FinalizeRegistries();
        //Register EntityRenderers


        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();



        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);


        base.Draw(gameTime);
    }

    
}
