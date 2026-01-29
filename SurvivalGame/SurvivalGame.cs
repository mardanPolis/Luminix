using System;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SurvivalGame.Core;
using SurvivalGame.Core.EntitySystem;
using SurvivalGame.Modding;

namespace SurvivalGame;

public sealed class SurvivalGame : Game
{
    public const string GameName = "SurvivalGame";
    public const string GameId = "sg";
    public const string ModPath = "mods";

    public static readonly string AppDataLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GameName);

    public static SurvivalGame Instance { private set; get; }
    public static readonly Assembly[] Assemblies = AppDomain.CurrentDomain.GetAssemblies();
    internal static readonly Logger LOGGER = Logger.GetLogger();

    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;

    public SurvivalGame()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        Directory.CreateDirectory(AppDataLoc);
        ModHandler.LoadMods();
        HandleRegistryInit();
        base.Initialize();
    }
    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        foreach(Mod mod in ModHandler.ActiveMods)
            mod.OnUnload();

        base.OnExiting(sender, args);
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        foreach(Mod mod in ModHandler.ActiveMods)
            mod.OnLoadContent();

    }
    protected override void UnloadContent()
    {
        foreach(Mod mod in ModHandler.ActiveMods)
            mod.OnUnloadContent();

        base.UnloadContent();
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



    private void HandleRegistryInit()
    {
        LOGGER.Log("Initializing Registry!");

        //Register Entity Tags
        GameRegistry.RegisterTags();

        Entities.FinalizeTagRegistry();

        //Register Entities
        GameRegistry.RegisterEntities();
        
        Entities.FinalizeEntityRegistry();

        //Register EntityRenderers
        GameRegistry.RegisterRenderers();

    }
}
