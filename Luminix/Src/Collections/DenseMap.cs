using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Luminix.Collections;

public sealed class DenseMap<T1, T2> : IEnumerable<(T1, T2)>, IReadOnlyCollection<(T1, T2)>, IReadOnlyDenseMap<T1, T2> where T1 : notnull where T2 : notnull
{
    public DenseMap(int capacity = 32)
    {
        items = new (T1, T2)[capacity];
        leftItems = new Dictionary<T1, int>();
        rightItems = new Dictionary<T2, int>();
        count = 0;
        defaultCapacity = Math.Max(capacity, 32);
    }

    public DenseMap(IEnumerable<(T1, T2)> source) : this(32)
    {
        foreach (var (left, right) in source)
            Add(left, right);
    }

    public DenseMap(
        int capacity = 32,
        IEqualityComparer<T1> leftComparer = null,
        IEqualityComparer<T2> rightComparer = null)
    {
        items = new (T1, T2)[capacity];
        leftItems = new Dictionary<T1, int>(leftComparer);
        rightItems = new Dictionary<T2, int>(rightComparer);
        count = 0;
        defaultCapacity = Math.Max(capacity, 32);
    }

    public DenseMap(
        IEnumerable<(T1, T2)> source,
        IEqualityComparer<T1> leftComparer = null,
        IEqualityComparer<T2> rightComparer = null)
        : this(32, leftComparer, rightComparer)
    {
        foreach (var (left, right) in source)
            Add(left, right);
    }

    private (T1 left, T2 right)[] items;
    private Dictionary<T1, int> leftItems;
    private Dictionary<T2, int> rightItems;

    private int count;
    private readonly int defaultCapacity;

    public int Count => count;
    public int Capacity => items.Length;
    public bool IsReadOnly => false;

    public LeftEnumerable LeftItems => new(this);
    public RightEnumerable RightItems => new(this);
    
    public T2 this[T1 left]
    {
        get
        {
            if (!leftItems.TryGetValue(left, out int index))
                throw new KeyNotFoundException($"Left item '{left}' was not found.");

            return items[index].right;
        }
    }

    public T1 this[T2 right]
    {
        get
        {
            if (!rightItems.TryGetValue(right, out int index))
                throw new KeyNotFoundException($"Left item '{right}' was not found.");

            return items[index].left;
        }
    }

    public (T1, T2) this[int index]
    {
        get
        {
            if (index < 0 || index >= count)
                throw new IndexOutOfRangeException($"Index {index} is out of range for DenseMap with count {count}.");

            return items[index];
        }
    }

    public void Add(T1 left, T2 right)
    {
        if (leftItems.ContainsKey(left))
            throw new ArgumentException($"Left key '{left}' already exists.");
        if (rightItems.ContainsKey(right))
            throw new ArgumentException($"Right key '{right}' already exists.");

        if (count >= items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[count] = (left, right);
        leftItems[left] = count;
        rightItems[right] = count;
        count++;
    }

    public bool TryAdd(T1 left, T2 right)
    {
        if (leftItems.ContainsKey(left) || rightItems.ContainsKey(right))
            return false;

        if (count >= items.Length)
            Array.Resize(ref items, items.Length * 2);

        items[count] = (left, right);
        leftItems[left] = count;
        rightItems[right] = count;
        count++;
        return true;
    }

    public bool Contains((T1, T2) item)
    {
        return leftItems.TryGetValue(item.Item1, out int index)
               && EqualityComparer<T2>.Default.Equals(items[index].Item2, item.Item2);
    }

    public bool ContainsLeft(T1 key) => leftItems.ContainsKey(key);
    public bool ContainsRight(T2 key) => rightItems.ContainsKey(key);

    public bool TryGetLeft(T2 key, [MaybeNullWhen(false)] out T1 value)
    {
        if (rightItems.TryGetValue(key, out int index))
        {
            value = items[index].Item1;
            return true;
        }

        value = default;
        return false;
    }

    public bool TryGetRight(T1 key, [MaybeNullWhen(false)] out T2 value)
    {
        if (leftItems.TryGetValue(key, out int index))
        {
            value = items[index].Item2;
            return true;
        }

        value = default;
        return false;
    }

    public void CopyTo((T1, T2)[] array, int arrayIndex)
    {
        Array.Copy(items, 0, array, arrayIndex, count);
    }

    private void RemoveAt(int index)
    {
        int lastIndex = count - 1;
        var removed = items[index];

        if (index < lastIndex)
        {
            var last = items[lastIndex];
            items[index] = last;
            leftItems[last.Item1] = index;
            rightItems[last.Item2] = index;
        }

        items[lastIndex] = default!;
        leftItems.Remove(removed.Item1);
        rightItems.Remove(removed.Item2);
        count--;
    }

    public bool RemoveLeft(T1 key)
    {
        if (!leftItems.TryGetValue(key, out int index))
            return false;

        RemoveAt(index);
        return true;
    }

    public bool RemoveRight(T2 key)
    {
        if (!rightItems.TryGetValue(key, out int index))
            return false;

        RemoveAt(index);
        return true;
    }

    public bool Remove((T1, T2) item)
    {
        if (!leftItems.TryGetValue(item.Item1, out int index))
            return false;

        if (!EqualityComparer<T2>.Default.Equals(items[index].Item2, item.Item2))
            return false;

        RemoveAt(index);
        return true;
    }

    public void Clear()
    {
        Array.Clear(items, 0, count);
        leftItems.Clear();
        rightItems.Clear();
        count = 0;
    }

    public void Reset()
    {
        Clear();
        items = new (T1, T2)[defaultCapacity];
        leftItems.TrimExcess(defaultCapacity);
        rightItems.TrimExcess(defaultCapacity);
    }

    public void TrimExcess()
    {
        Array.Resize(ref items, count);
        leftItems.TrimExcess();
        rightItems.TrimExcess();
    }

    public void TrimExcess(int capacity)
    {
        if (capacity < count)
            throw new ArgumentOutOfRangeException(nameof(capacity), "capacity cannot be less than Count.");

        Array.Resize(ref items, capacity);
        leftItems.TrimExcess(capacity);
        rightItems.TrimExcess(capacity);
    }

    public DenseMapEnumerator GetEnumerator() => new(this);

    IEnumerator<(T1, T2)> IEnumerable<(T1, T2)>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct DenseMapEnumerator : IEnumerator<(T1, T2)>
    {
        public DenseMapEnumerator(DenseMap<T1, T2> map)
        {
            this.map = map;
            index = -1;
            current = default;
        }

        private readonly DenseMap<T1, T2> map;
        private int index;
        private (T1, T2) current;

        public (T1, T2) Current => current;
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (index + 1 >= map.count)
                return false;

            index++;
            current = map.items[index];
            return true;
        }

        public void Reset()
        {
            index = -1;
            current = default;
        }

        public void Dispose() => current = default;
    }


    public readonly struct LeftEnumerable : IEnumerable<T1>
    {
        private readonly DenseMap<T1, T2> map;
        public LeftEnumerable(DenseMap<T1, T2> map) => this.map = map;

        public LeftEnumerator GetEnumerator() => new(map);
        IEnumerator<T1> IEnumerable<T1>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct LeftEnumerator : IEnumerator<T1>
    {
        public LeftEnumerator(DenseMap<T1, T2> map)
        {
            this.map = map;
            index = -1;
            current = default!;
        }

        private readonly DenseMap<T1, T2> map;
        private int index;
        private T1 current;

        public T1 Current => current;
        object IEnumerator.Current => Current!;

        public bool MoveNext()
        {
            if (index + 1 >= map.count)
                return false;

            index++;
            current = map.items[index].Item1;
            return true;
        }

        public void Reset()
        {
            index = -1;
            current = default!;
        }

        public void Dispose() => current = default!;
    }

    public readonly struct RightEnumerable : IEnumerable<T2>
    {
        private readonly DenseMap<T1, T2> map;
        public RightEnumerable(DenseMap<T1, T2> map) => this.map = map;

        public RightEnumerator GetEnumerator() => new(map);
        IEnumerator<T2> IEnumerable<T2>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct RightEnumerator : IEnumerator<T2>
    {
        public RightEnumerator(DenseMap<T1, T2> map)
        {
            this.map = map;
            index = -1;
            current = default!;
        }

        private readonly DenseMap<T1, T2> map;
        private int index;
        private T2 current;

        public T2 Current => current;
        object IEnumerator.Current => Current!;

        public bool MoveNext()
        {
            if (index + 1 >= map.count)
                return false;

            index++;
            current = map.items[index].Item2;
            return true;
        }

        public void Reset()
        {
            index = -1;
            current = default!;
        }

        public void Dispose() => current = default!;
    }
}