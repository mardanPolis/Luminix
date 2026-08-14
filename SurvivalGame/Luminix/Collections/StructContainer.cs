using System;

namespace Luminix.Collections;

internal class StructContainer<T> : IStructContainer where T : struct
{
    public Type StructType => typeof(T);
    public int Length => data.Length;

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
        data[index] = default;
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