
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Xna.Framework;

namespace Luminix.Threading.Bridges;

internal class AssetBridge
{
    private struct TextureAsset(string name, Color[] colors, Vector2 scale)
    {
        public string Name = name;
        public Color[] Colors = colors;
        public Vector2 Scale = scale;
    }

    private struct TextureRegionAsset(string name, string textureName, Rectangle rectangle)
    {
        public string Name = name;
        public string TextureName = textureName;
        public Rectangle Rectangle = rectangle;
    }

    private struct StreamAsset(string name, Stream stream)
    {
        public string Name = name;
        public Stream Stream = stream;
    }

    private ConcurrentQueue<TextureAsset> textureAssets;
    private ConcurrentQueue<TextureRegionAsset> textureRegionAssets;
    private ConcurrentQueue<StreamAsset> assets;

    public void RegisterTexture(string name, Color[] colors, Vector2 scale)
    {
        textureAssets.Enqueue(new TextureAsset(name, colors, scale));
    }

    public void RegisterTextureRegion(string name, string textureName, Vector2 position, Vector2 size)
    {
        textureRegionAssets.Enqueue(new TextureRegionAsset(name, textureName, new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y)));
    }

    public void RegisterTextureRegion(string name, string textureName, Rectangle rectangle)
    {
        textureRegionAssets.Enqueue(new TextureRegionAsset(name, textureName, rectangle));
    }

    public void RegisterStream(string name, Stream stream)
    {
        assets.Enqueue(new StreamAsset(name, stream));
    }
}