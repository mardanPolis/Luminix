using System;

namespace Luminix.Collections;

internal interface IStructContainer
{
    public Type StructType { get; }
    public int StructSize { get; }
    public int Length { get; }
    public Array GetArray();
    public void Resize(int newLength);
    public void Default(int index);
    public void Swap(int index, int otherIndex);
    public void Reset(int capacity);
}
