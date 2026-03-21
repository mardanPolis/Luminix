using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Verse.Rendering;

namespace Verse.Resources;
public static class ResourcesManager
{
    private static readonly Dictionary<string, TextureRegion> sprites = new Dictionary<string, TextureRegion>();
    private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    
    public static ContentManager ContentManager { get; private set; }

    public static void Initialize(ContentManager content)
    {
        ContentManager = content;
    }

    public static void LoadTexture(string name)
    {
        if (textures.ContainsKey(name))
            return;

        var texture = ContentManager.Load<Texture2D>(name);
        textures[name] = texture;
    }

    public static void NewSprite(string name, string textureName, int x, int y, int width, int height)
    {
        if (sprites.ContainsKey(name))
            return;

        if (!textures.TryGetValue(textureName, out var texture))
            throw new KeyNotFoundException($"Texture with name {textureName} not found.");

        var sprite = new TextureRegion(texture, x, y, width, height);
        sprites[name] = sprite;
        
    }

    public static TextureRegion GetSprite(string name)
    {
        if (!sprites.TryGetValue(name, out var sprite))
            throw new KeyNotFoundException($"Sprite with name {name} not found.");

        return sprite;
    }

}