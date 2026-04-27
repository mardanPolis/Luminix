using System;
using System.Collections;
using System.Collections.Generic;
using Verse.Collections;

namespace Verse.Collections;

public sealed class DenseList<T> : ICollection, IEnumerable<T>, IReadOnlyDenseList<T>
{
    public DenseList(int capacity = 16)
    {
        items = new T[capacity];
        indexMap = new Dictionary<T, int>();
        count = 0;
    }

    private T[] items;
    private int count;
    private readonly Dictionary<T, int> indexMap;

    public int Count => count;
    public bool IsSynchronized => false;
    public object SyncRoot => items;

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= count)
                throw new IndexOutOfRangeException($"Index {index} is out of range for DenseList<{typeof(T).Name}> with count {count}.");

            return items[index];
        }
        set
        {
            if (index < 0 || index >= count)
                throw new IndexOutOfRangeException($"Index {index} is out of range for DenseList<{typeof(T).Name}> with count {count}.");

            items[index] = value;
        }
    }
    
    public bool Add(T item)
    {
        if (indexMap.ContainsKey(item))
            return false;

        if (count >= items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[count] = item;
        indexMap[item] = count;
        count++;
        return true;
    }

    public bool Remove(T item)
    {
        if (!indexMap.TryGetValue(item, out int idx))
            return false;

        int lastIndex = count - 1;
        
        if (idx < lastIndex)
        {
            T lastItem = items[lastIndex];
            items[idx] = lastItem;
            indexMap[lastItem] = idx;
        }

        indexMap.Remove(item);
        items[lastIndex] = default!;
        count--;
        return true;
    }

    public bool Contains(T item) => indexMap.ContainsKey(item);

    public void Clear()
    {
        Array.Clear(items, 0, count);
        indexMap.Clear();
        count = 0;
    }

    public void CopyTo(Array array, int index)
    {
        items.CopyTo(array, index);
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < count; i++)
            yield return items[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public T[] ToArray()
    {
        T[] result = new T[count];
        Array.Copy(items, result, count);
        return result;
    }

    public List<T> ToList()
    {
        var array = ToArray();
        var result = new List<T>(array);
        return result;
    }
}
