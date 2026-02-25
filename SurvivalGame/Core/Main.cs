using System;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SurvivalGame.Core;
using SurvivalGame.Engine;
using SurvivalGame.Engine.EntitySystem;
using SurvivalGame.Engine.Modding;
using SurvivalGame.Modding;

namespace SurvivalGame;

public sealed class Main : Game
{
    public const string GAME_NAME = "SurvivalGame";
    public const string GAME_ID = "survival_game";
    public const string MODS_PATH = "mods";

    public static EngineState CurrentGameState { private set; get; } = EngineState.Initialization;

    public static readonly string AppDataLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GAME_NAME);

    public static Main Instance { private set; get; }
    public static readonly Assembly[] Assemblies = AppDomain.CurrentDomain.GetAssemblies();
    internal static readonly Logger LOGGER = Logger.Get();

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
        Directory.CreateDirectory(AppDataLoc);
        ModHandler.LoadMods();
        HandleRegistryInit();
        base.Initialize();
    }
    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        foreach (Mod mod in ModHandler.ActiveMods)
            mod.OnUnload();

        base.OnExiting(sender, args);
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        foreach (Mod mod in ModHandler.ActiveMods)
            mod.OnLoadContent();

    }
    protected override void UnloadContent()
    {
        foreach (Mod mod in ModHandler.ActiveMods)
            mod.OnUnloadContent();

        base.UnloadContent();
    }

    protected override void BeginRun()
    {
        CurrentGameState = EngineState.Runtime;

        GameRegistry.SetUp();

        foreach (var typeValues in EntityTypeRegistry.RegistryTypeValues)
            typeValues.Init();

        base.BeginRun();
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

        EntityTagRegistry.FinalizeRegistry();

        //Register Entities
        EntityTypeRegistry.SetPrefix(GAME_ID);
        GameRegistry.RegisterEntities();

        EntityTypeRegistry.FinalizeRegistry();

        //Register EntityRenderers
        GameRegistry.RegisterRenderers();

        EntityFieldHandler.CreateAccessors();
    }
}
