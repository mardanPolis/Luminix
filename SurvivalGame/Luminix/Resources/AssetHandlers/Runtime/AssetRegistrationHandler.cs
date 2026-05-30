
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Luminix.IO;
using Luminix.Threading.Bridges;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace Luminix.Resources.AssetHandlers;

internal class AssetRegistrationHandler
{
    public AssetRegistrationHandler(AssetBridge assetBridge, ResourceManager resourceManager)
    {
        ArgumentNullException.ThrowIfNull(assetBridge, nameof(assetBridge));
        ArgumentNullException.ThrowIfNull(resourceManager, nameof(resourceManager));

        this.assetBridge = assetBridge;
        this.resourceManager = resourceManager;

        action = (asset) =>
        {
            if(asset is TextureAsset textureAsset)
            {
                resourceManager.CreateTexture(textureAsset.Name, textureAsset.Colors, textureAsset.Size);
                resourceManager.CreateTextureRegion(textureAsset.Name, textureAsset.Name, new Rectangle(0, 0, (int)textureAsset.Size.X, (int)textureAsset.Size.Y));
            }
            else if(asset is AtlasAsset atlasAsset)
            {
                resourceManager.CreateTexture(atlasAsset.Name, atlasAsset.Colors, atlasAsset.Size);
                for(int i = 0; i < atlasAsset.Regions.Length; i++)
                {
                    var region = atlasAsset.Regions[i];
                    resourceManager.CreateTextureRegion(atlasAsset.Name, region.Name, region.Rectangle);
                }
            }
            else if(asset is AudioAsset audioAsset)
            {
                resourceManager.CreateSound(audioAsset.Name, audioAsset.SoundEffect);
            }
            else if(asset is StreamAsset streamAsset)
            {
                switch(streamAsset.Type)
                {
                    case StreamAsset.AssetType.Texture:
                        resourceManager.Create<Texture2D>(streamAsset.Name, streamAsset.Stream);
                    break;
                    case StreamAsset.AssetType.Font:
                        resourceManager.Create<SpriteFont>(streamAsset.Name, streamAsset.Stream);
                    break;
                    case StreamAsset.AssetType.Audio:
                        resourceManager.Create<SoundEffect>(streamAsset.Name, streamAsset.Stream);
                    break;
                    case StreamAsset.AssetType.Shader:
                        resourceManager.Create<Effect>(streamAsset.Name, streamAsset.Stream);
                    break;
                }
            }
        };
    }

    private readonly AssetBridge assetBridge;
    private readonly Action<Asset> action;

    private readonly ResourceManager resourceManager;

    public void Run()
    {
        assetBridge.ConsumeAssets(action, out List<TaskCompletionSource> tsks);
        RenderToCore.ResourceBridge.Set(resourceManager.Resources);
        for(int i = 0; i < tsks.Count; i++)
            tsks[i].SetResult();
    }
}