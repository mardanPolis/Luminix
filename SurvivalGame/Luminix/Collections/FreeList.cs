using System;
using System.Collections;
using System.Collections.Generic;
using Luminix.Identifiers;

namespace Luminix.Collections;

public sealed class FreeList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, ICollection where T : class
{
    private T[] items;
    private readonly Stack<uint> freeSlots;
    private int nextAvailableIndex;
    public FreeList(int capacity = 32)
    {
        items = new T[capacity];
        freeSlots = new Stack<uint>();
        nextAvailableIndex = 0;
    }

    public int Capacity => items.Length;
    
    public int Count => nextAvailableIndex - freeSlots.Count;
    public uint Cursor => (uint)nextAvailableIndex;

    public bool IsSynchronized => false;
    public object SyncRoot => items;
    public bool IsReadOnly => false;

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= nextAvailableIndex)
                throw new IndexOutOfRangeException($"Index {index} is out of range for FreeList<{typeof(T).Name}> with Cursor {Cursor}.");

            return items[index];
        }
    }

    void ICollection<T>.Add(T item)
    {
        Add(item);
    }

    public ID Add(T item)
    {
        if (nextAvailableIndex < items.Length)
        {
            uint idx = (uint)nextAvailableIndex++;
            items[idx] = item;
            return (ID)idx;
        }

        if (freeSlots.Count > 0)
        {
            uint reusedIdx = freeSlots.Pop();
            items[reusedIdx] = item;
            return (ID)reusedIdx;
        }

        Array.Resize(ref items, items.Length * 2);

        uint newIdx = (uint)nextAvailableIndex++;
        items[newIdx] = item;
        return (ID)newIdx;
    }

    public bool Remove(T item)
    {
        throw new NotImplementedException();
    }

    public void Remove(ID id)
    {
        var value = id.Value;
        if (value < 0 || value >= nextAvailableIndex) return;
        
        items[value] = null;
        
        freeSlots.Push(value);
    }

    public bool Contains(IIdentifiable identifiable)
    {
        ArgumentNullException.ThrowIfNull(identifiable);
        
        if(identifiable is not T)
            return false;

        var id = identifiable.GetID();

        if (id.Value >= nextAvailableIndex)
            return false;

        var itemAtSlot = items[id.Value];

        if (itemAtSlot is null)
            return false;

        return true;
    }

    public bool Contains(T item)
    {
        if (item == null) return false;

        if (item is IIdentifiable identifiable)
        {
            var id = identifiable.GetID();
            uint val = id.Value;

            if (val < (uint)nextAvailableIndex && ReferenceEquals(items[val], item))
            {
                return true;
            }
        }

        return GetItemID(item) != null;
    }

    public bool Contains(T item, out ID _id)
    {
        _id = new ID();
        if (item == null) return false;

        if (item is IIdentifiable identifiable)
        {
            _id = identifiable.GetID();
            uint val = _id.Value;

            if (val < (uint)nextAvailableIndex && ReferenceEquals(items[val], item))
            {
                return true;
            }
        }

        var id = GetItemID(item);
        var contains = id.HasValue;
        if(contains)
            _id = id.Value;

        return contains;
    }

    public ID? GetItemID(T item)
    {
        for (int i = 0; i < nextAvailableIndex; i++)
        {
            if (ReferenceEquals(items[i], item))
                return (ID)i;
        }
        return null;
    }

    public void Clear()
    {
        Array.Clear(items, 0, nextAvailableIndex);
        freeSlots.Clear();
        nextAvailableIndex = 0;
    }

    public void CopyTo(Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (index < 0 || index > array.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (array.Length - index < Count) throw new ArgumentException("Target array too small.");

        int currentTargetIndex = index;

        for (int i = 0; i < nextAvailableIndex; i++)
        {
            T item = items[i];

            if (item != null)
                array.SetValue(item, currentTargetIndex++);
        }
    }

    public void CopyTo(T[] array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (index < 0 || index > array.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (array.Length - index < Count) throw new ArgumentException("Target array too small.");

        int currentTarget = index;
        for (int i = 0; i < nextAvailableIndex; i++)
        {
            if (items[i] != null)
                array[currentTarget++] = items[i];
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < nextAvailableIndex; i++)
        {
            if (items[i] is not null)
            {
                yield return items[i];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}