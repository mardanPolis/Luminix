using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Luminix.Rendering.RenderInstances;
using Microsoft.Xna.Framework;

namespace Luminix.Threading.Buffers;

public sealed class RenderBuffer
{
    internal enum RenderObjectType : byte
    {
        SpriteNonInterpolated,
        SpriteInterpolated,
        TextNonInterpolated,
        TextInterpolated,
    }
    internal enum UiRenderObjectType : byte
    {
        Image,
        Label,
    }

    internal readonly record struct RenderCommand(ulong SortKey, RenderObjectType Type, int DataIndex) : IComparable<RenderCommand>
    {
        public int CompareTo(RenderCommand other) => SortKey.CompareTo(other.SortKey);
    }
    internal readonly record struct UiRenderCommand(UiRenderObjectType Type, int DataIndex);
   
    internal readonly record struct RenderGroup(Vector2 topLeft, Vector2 btmRight);

    internal RenderBuffer()
    {
        instanceSprites = new(2560);
        instanceTexts = new(128);
        interpolatedInstanceSprites = new(2560);
        interpolatedInstanceTexts = new(128);

        instanceImages = new(128);
        instanceLabels = new(128);

        renderGroups = new(512);

        worldRenderCommands = new(4096);
        uiRenderCommands = new(256);
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

    //Commands
    private readonly List<RenderCommand> worldRenderCommands;
    private readonly List<UiRenderCommand> uiRenderCommands;

    internal void Set(Camera camera)
    {
        cameraData = camera?.GetCameraSnapshot();
    }

    internal void PushSprite(ShaderResource shader, TextureResource texture, Color color, Vector3 position, Vector2 scale, float rotation)
    {
        ulong key = BuildSpriteKey(shader, texture);
 
        int dataIndex = instanceSprites.Count;
        instanceSprites.Add(new InstanceSprite(shader, texture, color, position, scale, rotation));
        NewCommand(key, RenderObjectType.SpriteNonInterpolated, dataIndex);
    }
    internal void PushInterpolatedSprite(ShaderResource shader, TextureResource texture, Color color, Vector3 position0, Vector3 position, Vector2 scale0, Vector2 scale, float rotation0, float rotation)
    {
        ulong key = BuildSpriteKey(shader, texture);
 
        int dataIndex = interpolatedInstanceSprites.Count;
        interpolatedInstanceSprites.Add(new InterpolatedInstanceSprite(shader, texture, color, position0, position, scale0, scale, rotation0, rotation));
        NewCommand(key, RenderObjectType.SpriteInterpolated, dataIndex);
    }


    internal void PushText(ShaderResource shader, FontResource font, Color color, string text, Vector3 position, Vector2 scale, float rotation)
    {
        ulong key = BuildFontKey(shader, font);
 
        int dataIndex = instanceTexts.Count;
        instanceTexts.Add(new InstanceText(shader, font, color, text, position, scale, rotation));
        NewCommand(key, RenderObjectType.TextNonInterpolated, dataIndex);
    }

    internal void PushInterpolatedText(ShaderResource shader, FontResource font, Color color, string text, Vector3 position0, Vector3 position, Vector2 scale0, Vector2 scale, float rotation0, float rotation)
    {
        ulong key = BuildFontKey(shader, font);
 
        int dataIndex = interpolatedInstanceTexts.Count;
        interpolatedInstanceTexts.Add(new InterpolatedInstanceText(shader, font, color, text, position0, position, scale0, scale, rotation0, rotation));
        NewCommand(key, RenderObjectType.TextInterpolated, dataIndex);
    }

    internal void PushImage(ShaderResource shader, TextureResource image, Color color, int x, int y, int width, int height, float rotation)
    {
        int dataIndex = instanceImages.Count;
        NewCommand(UiRenderObjectType.Image, dataIndex);
        instanceImages.Add(new InstanceImage(shader, image, color, x, y, width, height, rotation));
    }

    internal void PushLabel(ShaderResource shader, FontResource font, Color color, string text, int x, int y, int width, int height, float rotation)
    {
        int dataIndex = instanceLabels.Count;
        NewCommand(UiRenderObjectType.Label, dataIndex);
        instanceLabels.Add(new InstanceLabel(shader, font, color, text, x, y, width, height, rotation));
    }


    //TODO : Add RenderGroups, NonInterpolated only.
    internal void PushSpriteGroup()
    {
        
    }

    //TODO : Add PushBatch, batched sprites only.

    internal Camera.Data? GetCameraData()
        => cameraData;

    internal ReadOnlySpan<InstanceSprite> GetSprites() 
        => CollectionsMarshal.AsSpan(instanceSprites);

    internal ReadOnlySpan<InstanceText> GetTexts() 
        => CollectionsMarshal.AsSpan(instanceTexts);

    internal ReadOnlySpan<InterpolatedInstanceSprite> GetInterpolatedSprites() 
        => CollectionsMarshal.AsSpan(interpolatedInstanceSprites);

    internal ReadOnlySpan<InterpolatedInstanceText> GetInterpolatedTexts() 
        => CollectionsMarshal.AsSpan(interpolatedInstanceTexts);

    internal ReadOnlySpan<RenderGroup> GetGroups() 
        => CollectionsMarshal.AsSpan(renderGroups);

    internal ReadOnlySpan<InstanceImage> GetImages()
        => CollectionsMarshal.AsSpan(instanceImages);

    internal ReadOnlySpan<InstanceLabel> GetLabels()
        => CollectionsMarshal.AsSpan(instanceLabels);

    internal ReadOnlySpan<RenderCommand> GetWorldRenderCommands() 
        => CollectionsMarshal.AsSpan(worldRenderCommands);

    internal ReadOnlySpan<UiRenderCommand> GetUiRenderCommands() 
        => CollectionsMarshal.AsSpan(uiRenderCommands);

    internal void Sort()
    {
        worldRenderCommands.Sort();
    }

    internal void Reset()
    {
        worldRenderCommands.Clear();
        uiRenderCommands.Clear();

        instanceSprites.Clear();
        instanceTexts.Clear();
        interpolatedInstanceSprites.Clear();
        interpolatedInstanceTexts.Clear();

        instanceLabels.Clear();
        instanceImages.Clear();

        renderGroups.Clear();
    }

    private void NewCommand(ulong SortKey, RenderObjectType Type, int DataIndex)
    {
        worldRenderCommands.Add(new RenderCommand(SortKey, Type, DataIndex));
    }

    private void NewCommand(UiRenderObjectType Type, int DataIndex)
    {
        uiRenderCommands.Add(new UiRenderCommand(Type, DataIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong BuildSpriteKey(ShaderResource shader, TextureResource texture)
    {
        int shaderHash = RuntimeHelpers.GetHashCode(shader?.Source);
        int textureHash = RuntimeHelpers.GetHashCode(texture?.Source);

        uint shaderCode = (uint)shaderHash;
        uint textureCode = (uint)textureHash;

        ulong combinedHash = ((ulong)shaderCode << 32) | (ulong)textureCode;
        return combinedHash;
    }
 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong BuildFontKey(ShaderResource shader, FontResource font)
    {
        int shaderHash = RuntimeHelpers.GetHashCode(shader?.Source);
        int fontHash = RuntimeHelpers.GetHashCode(font?.Source);

        uint shaderCode = (uint)shaderHash;
        uint fontCode = (uint)fontHash;

        ulong combinedHash = ((ulong)shaderCode << 32) | (ulong)fontCode;
        return combinedHash;
    }
}

