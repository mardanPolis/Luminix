
using System;
using System.IO;
using Luminix.Threading.Bridges;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Luminix.Resources.AssetHandlers;

internal class AssetRegistrationHandler
{
    public AssetRegistrationHandler(AssetBridge assetBridge, ResourceManager resourceManager)
    {
        ArgumentNullException.ThrowIfNull(assetBridge, nameof(assetBridge));
        ArgumentNullException.ThrowIfNull(resourceManager, nameof(resourceManager));

        this.assetBridge = assetBridge;
        this.resourceManager = resourceManager;

        onTexture = (name, colors, size) =>
        {
            resourceManager.CreateTexture(name, colors, size);
        };

        onTextureRegion = (textureName, name, rectangle) =>
        {
            resourceManager.CreateTextureRegion(textureName, name, rectangle);
        };

        onStream = (name, stream) =>
        {
            
        };
    }

    private AssetBridge assetBridge;
    private ResourceManager resourceManager;

    private Action<string, Color[], Vector2> onTexture;
    private Action<string, string, Rectangle> onTextureRegion;
    private Action<string, SoundEffect> onSound;
    private Action<string, Stream> onStream;

    public void Run()
    {
        assetBridge.ConsumeAssets(onTexture, onTextureRegion, onSound, onStream);
    }
}