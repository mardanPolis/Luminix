using System;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SurvivalGame.Core;
using SurvivalGame.Core.EntitySystem;
using System.Collections.Generic;
using SurvivalGame.Modding;

namespace SurvivalGame;

public sealed class SurvivalGame : Game
{
    public static readonly string GameId = "survivalgame";
    public static SurvivalGame Instance { private set; get; }
    public static readonly Assembly[] Assemblies = AppDomain.CurrentDomain.GetAssemblies();
    internal static Logger Logger;

    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;

    public const string modPath = "mods";

    public List<ModInfo> loadedMods = null;
    public List<Assembly> modAssemblies = null;

    public SurvivalGame()
    {
        graphics = new GraphicsDeviceManager(this);
        Logger = new Logger();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        if(!Directory.Exists(modPath))
        {
            Directory.CreateDirectory(modPath);
            return;
        }
        
        loadedMods = ModLoader.LoadMods();

        //Register Entity Tags
        Entities.RegisterTag("invincible");
        Entities.FinalizeTagRegistry();
        //Register Entities

        Entities.Register("sg:some_entity", Entities.NewBuilder<EntityBuilder, SomeEntity>().Tagged("invincible", false));
        Entities.FinalizeEntityRegistry();
        //Register EntityRenderers

        SomeEntity someEntity = Entities.Create("sg:some_entity", new Vector2(0,0)) as SomeEntity;
        Console.WriteLine(someEntity.ToString());

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
