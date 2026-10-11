using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Luminix.Collections;

public sealed class DenseList<T> : ICollection<T>, IEnumerable<T>, IList<T>, IReadOnlyDenseList<T>, IEnumerable, ICollection where T : notnull
{
    private const int MIN_CAPACITY = 8;

    public DenseList() : this(MIN_CAPACITY) {}

    public DenseList(int capacity = MIN_CAPACITY)
    {
        capacity = Math.Max(capacity, MIN_CAPACITY);
        this.items = new T[capacity];
        this.indexDict = new Dictionary<T, int>(capacity);
        this.count = 0;
        this.defaultCapacity = capacity;
    }

    public DenseList(IEnumerable<T> source)
    {
        items = new T[MIN_CAPACITY];
        this.indexDict = new Dictionary<T, int>(MIN_CAPACITY);
        count = 0;
        defaultCapacity = MIN_CAPACITY;

        foreach (var item in source)
            Add(item);
    }

    public static DenseList<T> Empty = [];

    private T[] items;
    private int count;
    private readonly Dictionary<T, int> indexDict;

    private int defaultCapacity;

    public int Count => count;
    public int Capacity => items.Length;

    public bool IsSynchronized => false;
    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public object SyncRoot => items;

    public bool IsEmpty => count == 0;


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

            if (indexDict.ContainsKey(value))
                return;

            var oldItem = items[index];
            indexDict.Remove(oldItem);
            
            items[index] = value;
            indexDict[value] = index;
        }
    }
    
    public void Add(T item)
    {
        if (indexDict.ContainsKey(item))
            return;

        if (count >= items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[count] = item;
        indexDict[item] = count;
        count++;
    }
    public void AddRange(IReadOnlyList<T> source)
    {
        EnsureCapacity(source.Count);
        for (int i = 0; i < source.Count; i++)
            Add(source[i]);
    }

    public void AddRange(IEnumerable<T> source)
    {
        switch (source)
        {
            case IReadOnlyList<T> list: AddRange(list); return;
            case ICollection<T> collection:
                EnsureCapacity(count + collection.Count);
                break;
        }

        foreach (var item in source)
            Add(item);
    }

    public bool TryAdd(T item)
    {
        if (indexDict.ContainsKey(item))
            return false;

        if (count >= items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[count] = item;
        indexDict[item] = count;
        count++;
        return true;
    }

    public bool TryAdd(T item, out int index)
    {
        if (indexDict.TryGetValue(item, out index))
            return false;

        if (count >= items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[count] = item;
        indexDict[item] = count;
        index = count;
        count++;
        return true;
    }

    public void Insert(int index, T item)
    {
        if (indexDict.ContainsKey(item))
            return;
        
        if (index < 0 || index > count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (count >= items.Length)
            Array.Resize(ref items, items.Length * 2);

        Array.Copy(items, index, items, index + 1, count - index); // moves items

        items[index] = item;
        count++;

        for (int i = index; i < count; i++)
            indexDict[items[i]] = i;
    }

    private void Remove(int index, T item)
    {
        int lastIndex = count - 1;
        
        if (index < lastIndex)
        {
            T lastItem = items[lastIndex];
            items[index] = lastItem;
            indexDict[lastItem] = index;
        }

        items[lastIndex] = default!;
        indexDict.Remove(item);
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
        if (!indexDict.TryGetValue(item, out int idx))
            return false;

        Remove(idx, item);
        return true;
    }

    public bool Contains(T item) 
    {
        return indexDict.ContainsKey(item);
    }
    public int IndexOf(T item)
    {
        return indexDict.TryGetValue(item, out int index) ? index : -1;
    }

    public void EnsureCapacity(int capacity)
    {
        if (capacity <= items.Length)
            return;

        int newCapacity = items.Length == 0 ? Math.Max(defaultCapacity, MIN_CAPACITY) : items.Length;
        while (newCapacity < capacity)
            newCapacity *= 2;

        Array.Resize(ref items, newCapacity);
        indexDict.EnsureCapacity(newCapacity);
    }

    public void Clear()
    {
        Array.Clear(items, 0, count);
        indexDict.Clear();
        count = 0;
    }
    public void Reset()
    {
        Clear();
        items = new T[defaultCapacity];
        indexDict.TrimExcess(defaultCapacity);
    }

    public void CopyTo(T[] array, int index)
    {
        Array.Copy(items, 0, array, index, count);
    }
    public void CopyTo(Array array, int index)
    {
        Array.Copy(items, 0, array, index, count);
    }

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
        return indexDict.Keys.ToHashSet();
    }

    public Span<T> AsSpan()
    {
        return items.AsSpan(0, count);
    }

    ReadOnlySpan<T> IReadOnlyDenseList<T>.AsSpan()
    {
        return AsSpan();
    }

    public Enumerator GetEnumerator()
    {
        return new((DenseList<T>)this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        public Enumerator(DenseList<T> values)
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
