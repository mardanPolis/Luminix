using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Luminix.Resources;

namespace Luminix.Threading.Buffers;

internal abstract class RenderBuffer
{
    public enum RenderObjectType : byte
    {
        SpriteNonInterpolated,
        SpriteInterpolated,
        TextNonInterpolated,
        TextInterpolated,
    }

    public readonly record struct RenderCommand(ulong SortKey, RenderObjectType Type, int DataIndex) : IComparable<RenderCommand>
    {
        public int CompareTo(RenderCommand other) => SortKey.CompareTo(other.SortKey);
    }
    
    public RenderBuffer()
    {
        renderCommands = new(4096);
    }

    private bool sorted;

    private readonly List<RenderCommand> renderCommands;
    public ReadOnlySpan<RenderCommand> GetRenderCommands() 
        => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(renderCommands);

    protected void NewCommand(ulong SortKey, RenderObjectType Type, int DataIndex)
    {
        renderCommands.Add(new RenderCommand(SortKey, Type, DataIndex));
    }

    public void Sort()
    {
        if(sorted)
        {
            return;
        }

        renderCommands.Sort();
        sorted = true;
    }

    public virtual void Reset()
    {
        sorted = false;
        renderCommands.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong BuildSpriteKey(TextureResource texture)
    {
        return (ulong)RuntimeHelpers.GetHashCode(texture?.Source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ulong BuildSpriteKey(ShaderResource shader, TextureResource texture)
    {
        int shaderHash = RuntimeHelpers.GetHashCode(shader?.Source);
        int textureHash = RuntimeHelpers.GetHashCode(texture?.Source);

        uint shaderCode = (uint)shaderHash;
        uint textureCode = (uint)textureHash;

        ulong combinedHash = ((ulong)shaderCode << 32) | (ulong)textureCode;
        return combinedHash;
    }
 
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static uint BuildFontKey(FontResource font)
    {
        return (uint)RuntimeHelpers.GetHashCode(font.Source);
    }
}

