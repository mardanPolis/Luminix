
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Luminix.Threading.Bridges;

internal class AssetBridge
{
    private enum AssetType
    {
        Texture,
        TextureRegion,
        Audio,
        Stream,
    }

    private struct Asset
    {

        //Asset
        public string Name;
        public TaskCompletionSource Tcs;
        public AssetType Type;


        //Texture
        public Color[] Colors;
        public Vector2 Size;

        //TextureRegion
        public string TextureName;
        public Rectangle Rectangle;

        //Audio
        public SoundEffect SoundEffect;

        //Stream
        public Stream Stream;
    }

    private ConcurrentQueue<Asset> assets = new ConcurrentQueue<Asset>();


    public void RegisterTexture(string name, Color[] colors, Vector2 size)
    {
        var asset = new Asset();
        asset.Name = name;
        asset.Type = AssetType.Texture;
        asset.Colors = colors;
        asset.Size = size;
        assets.Enqueue(asset);
    }

    public void RegisterTextureRegion(string name, string textureName, Vector2 position, Vector2 size)
    {
        RegisterTextureRegion(name, textureName, new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y));
    }

    public void RegisterTextureRegion(string name, string textureName, Rectangle rectangle)
    {
        var asset = new Asset();
        asset.Name = name;
        asset.Type = AssetType.TextureRegion;
        asset.TextureName = textureName;
        asset.Rectangle = rectangle;
        assets.Enqueue(asset);
    }

    public void RegisterTextureWithRegion(string name, Color[] colors, Vector2 size)
    {
        RegisterTexture(name, colors, size);
        RegisterTextureRegion(name, name, new Rectangle(0, 0, (int)size.X, (int)size.Y));
    }
    
    public void RegisterAudio(string name, SoundEffect soundEffect)
    {
        var asset = new Asset();
        asset.Name = name;
        asset.Type = AssetType.Audio;
        asset.SoundEffect = soundEffect;
        assets.Enqueue(asset);
    }

    public void RegisterStream(string name, Stream stream)
    {
        var asset = new Asset();
        asset.Name = name;
        asset.Type = AssetType.Stream;
        asset.Stream = stream;
        assets.Enqueue(asset);
    }

    // ASYNC
    public Task RegisterTextureAsync(string name, Color[] colors, Vector2 size)
    {
        var asset = new Asset();
        asset.Name = name;
        asset.Type = AssetType.Texture;
        asset.Tcs = new TaskCompletionSource();
        asset.Colors = colors;
        asset.Size = size;
        assets.Enqueue(asset);
        return asset.Tcs.Task;
    }

    public Task RegisterTextureRegionAsync(string name, string textureName, Vector2 position, Vector2 size)
    {
        return RegisterTextureRegionAsync(name, textureName, new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y));
    }

    public Task RegisterTextureRegionAsync(string name, string textureName, Rectangle rectangle)
    {
        var asset = new Asset();
        asset.Name = name;
        asset.Type = AssetType.TextureRegion;
        asset.Tcs = new TaskCompletionSource();
        asset.TextureName = textureName;
        asset.Rectangle = rectangle;
        assets.Enqueue(asset);
        return asset.Tcs.Task;
    }

    public Task RegisterTextureWithRegionAsync(string name, Color[] colors, Vector2 size)
    {
        var texTask = RegisterTextureAsync(name, colors, size);
        var regTask = RegisterTextureRegionAsync(name, name, new Rectangle(0, 0, (int)size.X, (int)size.Y));
        
        return Task.WhenAll(texTask, regTask);
    }

    public Task RegisterAudioAsync(string name, SoundEffect soundEffect)
    {
        var asset = new Asset();
        asset.Name = name;
        asset.Type = AssetType.Audio;
        asset.Tcs = new TaskCompletionSource();
        asset.SoundEffect = soundEffect;
        assets.Enqueue(asset);
        return asset.Tcs.Task;
    }

    public Task RegisterStreamAsync(string name, Stream stream)
    {
        var asset = new Asset();
        asset.Name = name;
        asset.Type = AssetType.Stream;
        asset.Tcs = new TaskCompletionSource();
        asset.Stream = stream;
        assets.Enqueue(asset);
        return asset.Tcs.Task;
    }


    public void ConsumeAssets(
        Action<string, Color[], Vector2> onTexture,
        Action<string, string, Rectangle> onRegion,
        Action<string, SoundEffect> onAudio,
        Action<string, Stream> onStream)
    {
        while (assets.TryDequeue(out Asset asset))
        {
            switch (asset.Type)
            {
                case AssetType.Texture:      onTexture(asset.Name, asset.Colors, asset.Size);              break;
                case AssetType.TextureRegion: onRegion(asset.Name, asset.TextureName, asset.Rectangle);    break;
                case AssetType.Audio:         onAudio(asset.Name, asset.SoundEffect);                      break;
                case AssetType.Stream:        onStream(asset.Name, asset.Stream);                          break;
            }
            asset.Tcs?.SetResult();
        }
    }
}