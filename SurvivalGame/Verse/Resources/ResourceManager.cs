using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Verse.Rendering;

namespace Verse.Resources;
public static class ResourceManager
{
    private static readonly Dictionary<string, TextureRegion> textureRegions = new Dictionary<string, TextureRegion>();
    private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    
    public static ContentManager ContentManager { get; private set; }

    public static string RootDirectory {get; private set;}

    private static Texture2D nullTexture;
    private static TextureRegion nullTextureRegion;

    private static SpriteFont IBMPlexMono_Bold;
    private static SpriteFont IBMPlexMono_Medium;
    private static SpriteFont IBMPlexMono_Regular;

    private static bool finalized = false;

    internal static void Initialize(string rootDirectory, ContentManager content)
    {
        RootDirectory = rootDirectory;
        ContentManager = content;
    }

    internal static void LoadDefaultData()
    {
        Texture2D pixel = new Texture2D(RenderingSystem.GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        textures["pixel"] = pixel;

        nullTexture = LoadTexture("null_texture.png");
        nullTextureRegion = NewTextureRegion("null_texture", "null_texture.png", 0, 0, 32, 32);

        IBMPlexMono_Bold = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Bold");
        IBMPlexMono_Medium = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Medium");
        IBMPlexMono_Regular = ContentManager.Load<SpriteFont>("Fonts/IBMPlexMono-Regular");
    }

    public static Texture2D LoadTexture(string name)
    {
        if (textures.ContainsKey(name))
            return textures[name];
        else if (finalized)
        {
            Logger.Get().Wrn($"Failed to load texture with name '{name}'.");
            return nullTexture;
        }

        Texture2D texture;
        using (var stream = File.OpenRead(Path.Combine(RootDirectory, name)))
            texture = Texture2D.FromStream(RenderingSystem.GraphicsDevice, stream);

        if (texture == null)
        {
            Logger.Get().Wrn($"Failed to load texture with name '{name}'.");
            return nullTexture;
        }

        textures[name] = texture;
        return texture;
    }

    public static TextureRegion NewTextureRegion(string name, string textureName, int x, int y, int width, int height)
    {
        if (textureRegions.ContainsKey(name))
            return null;

        if (!textures.TryGetValue(textureName, out var texture))
            throw new KeyNotFoundException($"Texture with name '{textureName}' not found.");

        var textureRegion = new TextureRegion(texture, x, y, width, height);
        textureRegions[name] = textureRegion;
        return textureRegion;
    }

    public static TextureRegion GetTextureRegion(string name)
    {
        if (!textureRegions.TryGetValue(name, out var textureRegion))
        {
            Logger.Get().Wrn($"TextureRegion with name '{name}' not found.");
            return nullTextureRegion;
        }

        return textureRegion;
    }

    public static SpriteFont GenericFontBold => IBMPlexMono_Bold;
    public static SpriteFont GenericFontMedium => IBMPlexMono_Medium;
    public static SpriteFont GenericFontRegular => IBMPlexMono_Regular;

}