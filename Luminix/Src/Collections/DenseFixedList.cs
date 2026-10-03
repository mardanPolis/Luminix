using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
 
namespace Luminix.Collections;
 
/// <summary>
/// A <see cref="DenseList{T}"/> variant with a fixed capacity that never grows.
/// Items are unique. Adding to a full list throws (or returns false for TryAdd).
/// </summary>
public sealed class DenseFixedList<T> : ICollection<T>, IEnumerable<T>, IList<T>, IReadOnlyDenseList<T>, IEnumerable, ICollection
    where T : notnull
{
    public DenseFixedList(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
 
        items = new T[capacity];
        indexDict = new Dictionary<T, int>(capacity);
        count = 0;
    }
    public DenseFixedList(int capacity, IEnumerable<T> source) : this(capacity)
    {
        foreach (var item in source)
            Add(item);
    }
 
    private readonly T[] items;
    private readonly Dictionary<T, int> indexDict;
    private int count;
 
    public int Count => count;
    public int Capacity => items.Length;
    public int RemainingCapacity => items.Length - count;
 
    public bool IsSynchronized => false;
    public bool IsReadOnly => false;
    public bool IsFixedSize => true;
    public object SyncRoot => items;
 
    public bool IsEmpty => count == 0;
    public bool IsFull => count == items.Length;
 
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
                throw new ArgumentException("Item already exists at a different index.", nameof(value));

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
 
        ThrowIfFull();
 
        items[count] = item;
        indexDict[item] = count;
        count++;
    }
 
    public void AddRange(IEnumerable<T> source)
    {
        foreach (var item in source)
            Add(item);
    }
 
    public bool TryAdd(T item)
    {
        return TryAdd(item, out _);
    }
 
    public bool TryAdd(T item, out int index)
    {
        if (indexDict.TryGetValue(item, out index))
            return false;
 
        if (count >= items.Length)
        {
            index = -1;
            return false;
        }
 
        items[count] = item;
        indexDict[item] = count;
        index = count;
        count++;
        return true;
    }
 
    public void Insert(int index, T item)
    {
        if (index < 0 || index > count)
            throw new ArgumentOutOfRangeException(nameof(index));
 
        if (indexDict.ContainsKey(item))
            return;
 
        ThrowIfFull();
 
        Array.Copy(items, index, items, index + 1, count - index);
 
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
 
    public bool Contains(T item) => indexDict.ContainsKey(item);
 
    public int IndexOf(T item) => indexDict.TryGetValue(item, out int index) ? index : -1;
 
    public void Clear()
    {
        Array.Clear(items, 0, count);
        indexDict.Clear();
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
 
    public T[] ToArray()
    {
        T[] result = new T[count];
        Array.Copy(items, result, count);
        return result;
    }
 
    public List<T> ToList() => new(this);
 
    public HashSet<T> ToHashSet() => new(indexDict.Keys);
 
    public Span<T> AsSpan() => items.AsSpan(0, count);
 
    ReadOnlySpan<T> IReadOnlyDenseList<T>.AsSpan() => AsSpan();
 
    private void ThrowIfFull()
    {
        if (count >= items.Length)
            throw new InvalidOperationException($"DenseFixedList<{typeof(T).Name}> is full (capacity {items.Length}).");
    }
 
    public Enumerator GetEnumerator() => new(this);
 
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
 
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
 
    public struct Enumerator : IEnumerator<T>
    {
        public Enumerator(DenseFixedList<T> values)
        {
            this.values = values;
            index = -1;
            current = default!;
        }
 
        private readonly DenseFixedList<T> values;
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
            current = default!;
        }
 
        public void Dispose()
        {
            current = default!;
        }
    }
}
