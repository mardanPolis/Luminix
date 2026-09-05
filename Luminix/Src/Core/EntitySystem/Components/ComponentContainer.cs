using System;
using System.Runtime.CompilerServices;

namespace Luminix.Collections;

internal class ComponentContainer<T> : IComponentContainer where T : struct
{
    public ComponentContainer(ComponentType<T> componentType)
    {
        this.componentType = componentType;
    }

    public IComponentType ComponentType => componentType;
    public int Length => data.Length;

    private ComponentType<T> componentType;

    public T[] Data => data;

    private T[] data;


    public ref T this[int index] {
        get => ref data[index];
    }

    public Array GetArray()
    {
        return Data;
    }

    public void Resize(int newLength)
    {
        Array.Resize(ref data, newLength);
    }
    
    public void Default(int index)
    {
        data[index] = componentType.Default;
    }

    public void Swap(int index, int otherIndex)
    {
        T temp = data[index];
        data[index] = data[otherIndex];
        data[otherIndex] = temp;
    }
    public void Reset(int capacity)
    {
        data = new T[capacity];
    }
}