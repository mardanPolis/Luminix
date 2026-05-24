using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Luminix.Collections;
using Luminix.Identifiers;

namespace Luminix.Resources;

public sealed class ResourceManager : Microsoft.Xna.Framework.Content.ContentManager
{    
    
    internal ResourceManager(Main main) : base(main.Services)
    {
        // resources = new Dictionary<string, Resource>();

        // RootDirectory = null;

        // MethodInfo method = typeof(Microsoft.Xna.Framework.Content.ContentManager).GetMethod("RecordDisposable", BindingFlags.Instance | BindingFlags.NonPublic) ??
        //  throw new InvalidOperationException("Could not find method RecordDisposable on ContentManager.");
        // RecordDisposable = (Action<IDisposable>)Delegate.CreateDelegate(typeof(Action<IDisposable>), this, method);

        // this.main = main;
        // this.graphicsDevice = main.GraphicsDevice;
    }
}


// NullTexture = LoadTexture("null_texture.png");
// NullTextureRegion = NewTextureRegion("null_texture", "null_texture.png", 0, 0, 32, 32);

// GenericFontBold = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Bold");
// GenericFontMedium = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Medium");
// GenericFontRegular = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Regular");

// TileShader = ContentManager.Load<Effect>("Effects/SpriteShader");