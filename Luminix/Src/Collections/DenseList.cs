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
        this.defaultCapacity = Math.Max(capacity, 32);
    }

    public DenseList(IEnumerable<T> source)
    {
        items = new T[defaultCapacity > 0 ? defaultCapacity : 32];
        indexMap = new Dictionary<T, int>();
        count = 0;
        defaultCapacity = Math.Max(items.Length, 32);

        foreach (var item in source)
            Add(item);
    }

    private T[] items;
    private int count;
    private readonly Dictionary<T, int> indexMap;

    private int defaultCapacity;

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
        index = count;
        count++;
        return true;
    }

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
    public void Reset()
    {
        Clear();
        items = new T[defaultCapacity];
        indexMap.TrimExcess(defaultCapacity);
    }

    public void CopyTo(T[] array, int index)
    {
        Array.Copy(items, 0, array, index, count);
    }
    public void CopyTo(Array array, int index)
    {
        Array.Copy(items, 0, array, index, count);
    }

    public DenseListEnumerator GetEnumerator()
    {
        return new DenseListEnumerator(this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
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

    public struct DenseListEnumerator : IEnumerator<T>
    {
        public DenseListEnumerator(DenseList<T> values)
        {
            this.index = -1;
            this.current = default;
            this.values = values;
        }

        private readonly DenseList<T> values;
        private int index;
        private T current;

        public T Current => current;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (index + 1 >= values.Count)
                return false;

            index++;
            current = values[index];
            return true;
        }

        public void Reset()
        {
            index = -1;
            current = default;
        }

        public void Dispose()
        {
            current = default;
        }
    }
}
