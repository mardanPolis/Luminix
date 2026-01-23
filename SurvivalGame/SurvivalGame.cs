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
    public const string ModPath = "mods";
    public const string GameId = "sg";

    public static readonly string AppDataLoc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), GameName);

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
        HandleModsInit();
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

    }
    protected override void UnloadContent()
    {
        foreach(Mod mod in ModHandler.ActiveMods)
            mod.OnUnload();

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



    private void HandleModsInit()
    {
        if(!Directory.Exists(ModPath))
        {
            Directory.CreateDirectory(ModPath);
            return;
        }

        Console.WriteLine("Loading Mods!");
        
        //Getting every possible mods
        ModHandler.LoadMods();
        
    }
    private void HandleRegistryInit()
    {
        Console.WriteLine("Initializing Registry!");

        //Register Entity Tags
        RegisterTags();

        Entities.FinalizeTagRegistry();
        //Register Entities
        RegisterEntities();
        
        Entities.FinalizeEntityRegistry();
        //Register EntityRenderers

    }



    private void RegisterTags()
    {
        Entities.RegisterTag("invincible");
    }
    private void RegisterEntities()
    {
        Entities.Register("sg:some_entity", Entities.NewBuilder<EntityBuilder, SomeEntity>().Tagged("invincible", false));
    }
    private void RegisterRenderers()
    {
        
    }
}
