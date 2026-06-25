using System;

namespace Luminix;

internal interface IComponentContainer
{
    public Type ComponentType { get; }
    public Array GetArray();
    public void Resize(int newLength);
    public void Default(int index);
    public void Set(int index, int otherIndex);
    public void Reset(int capacity);
}

internal class ComponentContainers<T> : IComponentContainer where T : struct
{
    public Type ComponentType => typeof(T);
    
    private T[] components;

    public Array GetArray()
    {
        return components;
    }
    public void Resize(int newLength)
    {
        Array.Resize(ref components, newLength);
    }
    public void Default(int index)
    {
        components[index] = default;
    }
    public void Set(int index, int otherIndex)
    {
        components[index] = components[otherIndex];
    }
    public void Reset(int capacity)
    {
        components = new T[capacity];
    }
}