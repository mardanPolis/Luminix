using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Luminix.Identifiers;
using Luminix.Rendering.RenderInstances;
using Luminix.Resources;
using Luminix.Simulation;
using Microsoft.Xna.Framework;

namespace Luminix.Rendering;

internal sealed class RenderBuffer
{
    public enum RenderObjectType : byte
    {
        SpriteNonInterpolated,
        SpriteInterpolated,
        FontNonInterpolated,
        FontInterpolated,
    }

    public readonly record struct RenderGroup(Vector2 topLeft, Vector2 btmRight);
    public readonly record struct RenderCommand(uint SortKey, RenderObjectType type, int DataIndex) : IComparable<RenderCommand>
    {
        public int CompareTo(RenderCommand other) => SortKey.CompareTo(other.SortKey);
    }
    
    public RenderBuffer(ID iD)
    {
        BufferID = iD;
        instanceSprites = new(2560);
        instanceTexts = new(128);
        interpolatedInstanceSprites = new(2560);
        interpolatedInstanceTexts = new(128);

        renderCommands = new(instanceSprites.Count + instanceTexts.Count + interpolatedInstanceSprites.Count + interpolatedInstanceTexts.Count);
    }

    public readonly ID BufferID;
    private bool sorted;

    private Matrix viewMatrix;

    private readonly List<InstanceSprite>  instanceSprites;
    private readonly List<InstanceText>    instanceTexts;
 
    private readonly List<InterpolatedInstanceSprite>  interpolatedInstanceSprites;
    private readonly List<InterpolatedInstanceText>    interpolatedInstanceTexts;
 
    private readonly List<RenderCommand> renderCommands;
    private readonly List<RenderGroup>   renderGroups   = new(1024);

    public void Set(Camera camera)
    {
        viewMatrix = camera.GetViewMatrix();
    }

    public void PostSprite(TextureResource texture, Vector3 position, Vector2 size, float rotation)
    {
        uint key = BuildSpriteKey(texture);
 
        int dataIndex = instanceSprites.Count;
        instanceSprites.Add(new InstanceSprite(texture, position, size, rotation));
        renderCommands.Add(new RenderCommand(key, RenderObjectType.SpriteNonInterpolated, dataIndex));
    }

    public void PostText(FontResource font, string text, Vector3 position, Vector2 size, float rotation)
    {
        uint key = BuildFontKey(font);
 
        int dataIndex = instanceTexts.Count;
        instanceTexts.Add(new InstanceText(font, text, position, size, rotation));
        renderCommands.Add(new RenderCommand(key, RenderObjectType.SpriteNonInterpolated, dataIndex));
    }

    public void PostInterpolatedSprite(TextureResource texture, Vector3 position0, Vector3 position, Vector2 size0, Vector2 size, float rotation0, float rotation)
    {
        uint key = BuildSpriteKey(texture);
 
        int dataIndex = interpolatedInstanceSprites.Count;
        interpolatedInstanceSprites.Add(new InterpolatedInstanceSprite(texture, position0, position, size0, size, rotation0, rotation));
        renderCommands.Add(new RenderCommand(key, RenderObjectType.SpriteNonInterpolated, dataIndex));
    }

    public void PostInterpolatedText(FontResource font, string text, Vector3 position0, Vector3 position, Vector2 size0, Vector2 size, float rotation0, float rotation)
    {
        uint key = BuildFontKey(font);
 
        int dataIndex = interpolatedInstanceTexts.Count;
        interpolatedInstanceTexts.Add(new InterpolatedInstanceText(font, text, position0, position, size0, size, rotation0, rotation));
        renderCommands.Add(new RenderCommand(key, RenderObjectType.SpriteNonInterpolated, dataIndex));
    }


    //TODO : Add RenderGroups, NonInterpolated only.
    public void PostSpriteGroup()
    {
        
    }

    //TODO : Add PostBatch, batched sprites only.

    public Matrix GetViewMatrix()
        => viewMatrix;

    public ReadOnlySpan<RenderCommand> GetRenderCommands() 
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(renderCommands);

    public ReadOnlySpan<InstanceSprite> GetSprites() 
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(instanceSprites);

    public ReadOnlySpan<InstanceText> GetTexts() 
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(instanceTexts);

    public ReadOnlySpan<InterpolatedInstanceSprite> GetInterpolatedSprites() 
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(interpolatedInstanceSprites);

    public ReadOnlySpan<InterpolatedInstanceText> GetInterpolatedTexts() 
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(interpolatedInstanceTexts);

    public void Sort()
    {
        if(sorted)
        {
            return;
        }

        renderCommands.Sort();
        sorted = true;
    }

    public void Reset()
    {
        sorted = false;

        instanceSprites.Clear();
        instanceTexts.Clear();
        interpolatedInstanceSprites.Clear();
        interpolatedInstanceTexts.Clear();

        renderCommands.Clear();
        renderGroups.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint BuildSpriteKey(TextureResource texture)
    {
        return (uint)RuntimeHelpers.GetHashCode(texture.Source);
    }
 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint BuildFontKey(FontResource font)
    {
        return (uint)RuntimeHelpers.GetHashCode(font.Source);
    }
}

