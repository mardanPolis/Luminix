
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

    private struct TextureRegionAsset(string name, string textureName, Vector2 position, Vector2 scale)
    {
        public string Name = name;
        public string TextureName = textureName;
        public Vector2 Position = position;
        public Vector2 Scale = scale;
    }

    private struct Asset(string name, Stream stream)
    {
        public string Name = name;
        public Stream Stream = stream;
    }

    private ConcurrentQueue<TextureAsset> textureAssets;
    private ConcurrentQueue<TextureRegionAsset> textureRegionAssets;
    private ConcurrentQueue<Asset> assets;

    public void NewTextureAsset(string name, Color[] colors, Vector2 scale)
    {
        textureAssets.Enqueue(new TextureAsset(name, colors, scale));
    }

    public void NewTextureRegionAsset(string name, string textureName, Vector2 position, Vector2 scale)
    {
        textureRegionAssets.Enqueue(new TextureRegionAsset(name, textureName, position, scale));
    }

    public void NewAsset(string name, Stream stream)
    {
        assets.Enqueue(new Asset(name, stream));
    }
}