using System;

namespace Luminix.Collections;

internal class StructContainer<T> : IStructContainer where T : struct
{
    public Type StructType => typeof(T);
    public int Length => data.Length;

    private T[] data;

    public T this[int index] {
        set => data[index] = value;
        get => data[index];
    }

    public Array GetArray()
    {
        return data;
    }
    public void Resize(int newLength)
    {
        Array.Resize(ref data, newLength);
    }
    public void Default(int index)
    {
        data[index] = default;
    }
    public void Set(int index, int otherIndex)
    {
        data[index] = data[otherIndex];
    }
    public void Reset(int capacity)
    {
        data = new T[capacity];
    }
}