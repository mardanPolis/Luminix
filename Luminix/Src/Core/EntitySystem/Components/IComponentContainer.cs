using System;

namespace Luminix;

internal interface IComponentContainer
{
    public IComponentType ComponentType { get; }
    public int Capacity { get; }
    public Array GetArray();
    public void Resize(int newLength);
    public void Default(int index);
    public void Swap(int index, int otherIndex);
    public void Reset(int capacity);
}
