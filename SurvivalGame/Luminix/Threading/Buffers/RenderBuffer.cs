using System;
using System.Collections.Generic;
using Luminix.Rendering.RenderInstances;
using Luminix.Resources;
using Microsoft.Xna.Framework;

namespace Luminix.Threading.Buffers;

internal sealed class RenderBuffer : BaseRenderBuffer
{
    public readonly record struct RenderGroup(Vector2 topLeft, Vector2 btmRight);

    public RenderBuffer()
    {
        instanceSprites = new(2560);
        instanceTexts = new(128);
        interpolatedInstanceSprites = new(2560);
        interpolatedInstanceTexts = new(128);

        renderGroups = new(512);
    }

    private Camera.Data? cameraData;

    //WorldSpace
    private readonly List<InstanceSprite>  instanceSprites;
    private readonly List<InstanceText>    instanceTexts;

    private readonly List<InterpolatedInstanceSprite>  interpolatedInstanceSprites;
    private readonly List<InterpolatedInstanceText>    interpolatedInstanceTexts;
 
    private readonly List<RenderGroup> renderGroups;

    //ScreenSpace
    private readonly List<InstanceImage> instanceImages;
    private readonly List<InstanceLabel> instanceLabels;

    public void Set(Camera camera)
    {
        cameraData = camera?.GetCameraSnapshot();
    }

    public void PushSprite(TextureResource texture, Vector3 position, Vector2 scale, float rotation)
    {
        ulong key = BuildSpriteKey(texture);
 
        int dataIndex = instanceSprites.Count;
        instanceSprites.Add(new InstanceSprite(null, texture, position, scale, rotation));
        NewCommand(key, RenderObjectType.SpriteNonInterpolated, dataIndex);
    }
    
    public void PushSprite(ShaderResource shader, TextureResource texture, Vector3 position, Vector2 scale, float rotation)
    {
        ulong key = BuildSpriteKey(shader, texture);
 
        int dataIndex = instanceSprites.Count;
        instanceSprites.Add(new InstanceSprite(shader, texture, position, scale, rotation));
        NewCommand(key, RenderObjectType.SpriteNonInterpolated, dataIndex);
    }

    public void PushText(FontResource font, string text, Vector3 position, Vector2 scale, float rotation)
    {
        uint key = BuildFontKey(font);
 
        int dataIndex = instanceTexts.Count;
        instanceTexts.Add(new InstanceText(font, text, position, scale, rotation));
        NewCommand(key, RenderObjectType.TextNonInterpolated, dataIndex);
    }

    public void PushInterpolatedSprite(TextureResource texture, Vector3 position0, Vector3 position, Vector2 scale0, Vector2 scale, float rotation0, float rotation)
    {
        ulong key = BuildSpriteKey(texture);
 
        int dataIndex = interpolatedInstanceSprites.Count;
        interpolatedInstanceSprites.Add(new InterpolatedInstanceSprite(null, texture, position0, position, scale0, scale, rotation0, rotation));
        NewCommand(key, RenderObjectType.SpriteInterpolated, dataIndex);
    }

    public void PushInterpolatedSprite(ShaderResource shader, TextureResource texture, Vector3 position0, Vector3 position, Vector2 scale0, Vector2 scale, float rotation0, float rotation)
    {
        ulong key = BuildSpriteKey(shader, texture);
 
        int dataIndex = interpolatedInstanceSprites.Count;
        interpolatedInstanceSprites.Add(new InterpolatedInstanceSprite(shader, texture, position0, position, scale0, scale, rotation0, rotation));
        NewCommand(key, RenderObjectType.SpriteInterpolated, dataIndex);
    }

    public void PushInterpolatedText(FontResource font, string text, Vector3 position0, Vector3 position, Vector2 scale0, Vector2 scale, float rotation0, float rotation)
    {
        uint key = BuildFontKey(font);
 
        int dataIndex = interpolatedInstanceTexts.Count;
        interpolatedInstanceTexts.Add(new InterpolatedInstanceText(font, text, position0, position, scale0, scale, rotation0, rotation));
        NewCommand(key, RenderObjectType.TextInterpolated, dataIndex);
    }

    public void PushImage(TextureResource image, int x, int y, int width, int height, float rotation)
    {
        instanceImages.Add(new InstanceImage(null, image, x, y, width, height, rotation));
    }
    public void PushImage(ShaderResource shader, TextureResource image, int x, int y, int width, int height, float rotation)
    {
        instanceImages.Add(new InstanceImage(shader, image, x, y, width, height, rotation));
    }

    public void PushLabel(FontResource font, string text, int x, int y, int width, int height, float rotation)
    {
        instanceLabels.Add(new InstanceLabel(font, text, x, y, width, height, rotation));
    }


    //TODO : Add RenderGroups, NonInterpolated only.
    public void PushSpriteGroup()
    {
        
    }

    //TODO : Add PushBatch, batched sprites only.

    public Camera.Data? GetCameraData()
        => cameraData;

    public ReadOnlySpan<InstanceSprite> GetSprites() 
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(instanceSprites);

    public ReadOnlySpan<InstanceText> GetTexts() 
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(instanceTexts);

    public ReadOnlySpan<InterpolatedInstanceSprite> GetInterpolatedSprites() 
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(interpolatedInstanceSprites);

    public ReadOnlySpan<InterpolatedInstanceText> GetInterpolatedTexts() 
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(interpolatedInstanceTexts);

    public ReadOnlySpan<RenderGroup> GetGroups() 
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(renderGroups);

    public ReadOnlySpan<InstanceImage> GetImages()
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(instanceImages);

    public ReadOnlySpan<InstanceLabel> GetLabels()
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(instanceLabels);

    public override void Reset()
    {
        base.Reset();

        instanceSprites.Clear();
        instanceTexts.Clear();
        interpolatedInstanceSprites.Clear();
        interpolatedInstanceTexts.Clear();

        renderGroups.Clear();
    }
}

