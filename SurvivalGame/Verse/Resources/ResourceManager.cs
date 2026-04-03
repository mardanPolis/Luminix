using System.Collections.Generic;
using System.IO;
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

    internal static void Initialize(string rootDirectory, ContentManager content)
    {
        RootDirectory = rootDirectory;
        ContentManager = content;
    }

    public static Texture2D LoadTexture(string name)
    {
        if (textures.ContainsKey(name))
            return textures[name];

        Texture2D texture;
        using (var stream = File.OpenRead(Path.Combine(RootDirectory, name)))
            texture = Texture2D.FromStream(RenderingSystem.GraphicsDevice, stream);

        if (texture == null)
            throw new FileNotFoundException($"Failed to load texture with name '{name}'.");

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
            throw new KeyNotFoundException($"TextureRegion with name '{name}' not found.");

        return textureRegion;
    }

}