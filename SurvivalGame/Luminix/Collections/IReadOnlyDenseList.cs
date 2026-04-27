using System;
using System.Collections.Generic;

namespace Luminix.Collections;

public interface IReadOnlyDenseList<T>
{
    public int Count { get; }
    public bool IsSynchronized { get; }
    public object SyncRoot { get; }

    public T this[int index] { get; }

    public bool Contains(T item);

    public Type GetValueType()
    {
        return typeof(T);
    }

    public T[] ToArray();
    public List<T> ToList();
}
