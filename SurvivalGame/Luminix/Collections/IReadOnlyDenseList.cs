using System;
using System.Collections;
using System.Collections.Generic;

namespace Luminix.Collections;

public interface IReadOnlyDenseList<T> : IReadOnlyCollection<T>, IReadOnlyList<T>
{
    public bool Contains(T item);
    public int IndexOf(T item);

    public Type GetValueType()
    {
        return typeof(T);
    }

    public T[] ToArray();
    public List<T> ToList();
}
