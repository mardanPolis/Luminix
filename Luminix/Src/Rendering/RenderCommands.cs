using System;
namespace Luminix.Rendering;
internal record struct RenderCommand(ulong Key, int Index) : IComparable<RenderCommand>
{
    public int CompareTo(RenderCommand other) => Key.CompareTo(other.Key);
}