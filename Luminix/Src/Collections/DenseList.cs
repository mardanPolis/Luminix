using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Luminix.Collections;

public sealed class DenseList<T> : ICollection<T>, IEnumerable<T>, IList<T>, IReadOnlyDenseList<T>, IEnumerable, ICollection
{
    public DenseList(int capacity = 32)
    {
        this.items = new T[capacity];
        this.indexMap = new Dictionary<T, int>();
        this.count = 0;
    }

    public DenseList(IEnumerable<T> source)
    {
        items = source.ToArray();
    }

    private T[] items;
    private int count;
    private readonly Dictionary<T, int> indexMap;

    public int Count => count;
    public int Capacity => items.Length;

    public bool IsSynchronized => false;
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
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

            var oldItem = items[index];
            indexMap.Remove(oldItem);
            
            items[index] = value;
            indexMap[value] = index;
        }
    }
    
    public void Add(T item)
    {
        if (indexMap.ContainsKey(item))
            return;

        if (count >= items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[count] = item;
        indexMap[item] = count;
        count++;
    }
    public bool TryAdd(T item)
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

    public bool TryAdd(T item, out int index)
    {
        if (indexMap.TryGetValue(item, out index))
            return false;

        if (count >= items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[count] = item;
        indexMap[item] = count;
        count++;
        index = count;
        return true;
    }

    /// <remarks>
    /// <b>Complexity: O(2n)</b>. <br/>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if index is out of bounds.</exception>
    public void Insert(int index, T item)
    {
        if (indexMap.ContainsKey(item))
            return;
        
        if (index < 0 || index > count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (count >= items.Length)
            Array.Resize(ref items, items.Length * 2);

        Array.Copy(items, index, items, index + 1, count - index);

        items[index] = item;
        count++;

        for (int i = index; i < count; i++)
            indexMap[items[i]] = i;
    }

    private void Remove(int index, T item)
    {
        int lastIndex = count - 1;
        
        if (index < lastIndex)
        {
            T lastItem = items[lastIndex];
            items[index] = lastItem;
            indexMap[lastItem] = index;
        }

        items[lastIndex] = default!;
        indexMap.Remove(item);
        count--;
    }
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(nameof(index));

        Remove(index, items[index]);
    }

    public bool Remove(T item)
    {
        if (!indexMap.TryGetValue(item, out int idx))
            return false;

        Remove(idx, item);
        return true;
    }

    public bool Contains(T item) 
    {
        return indexMap.ContainsKey(item);
    }
    public int IndexOf(T item)
    {
        return indexMap.TryGetValue(item, out int index) ? index : -1;
    }

    public void Clear()
    {
        Array.Clear(items, 0, count);
        indexMap.Clear();
        count = 0;
    }
    public void CopyTo(T[] array, int index)
    {
        Array.Copy(items, 0, array, index, count);
    }
    public void CopyTo(Array array, int index)
    {
        Array.Copy(items, 0, array, index, count);
    }

    //TODO not efficient

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
        var result = new List<T>(this);
        return result;
    }

    public HashSet<T> ToHashSet()
    {
        return indexMap.Keys.ToHashSet();
    }
}
