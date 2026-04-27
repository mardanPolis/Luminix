using System;
using System.Collections;
using System.Collections.Generic;

namespace Luminix.Collections;

public class UniqueQueue<T> : ICollection
{
    private Queue<T> queue = new Queue<T>();
    private HashSet<T> set = new HashSet<T>();

    public int Count => queue.Count;
    public bool IsSynchronized => false;
    public object SyncRoot => queue;

    public bool Enqueue(T item)
    {
        if (set.Contains(item))
            return false;

        queue.Enqueue(item);
        set.Add(item);
        return true;
    }

    public T Dequeue()
    {
        if (queue.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        T item = queue.Dequeue();
        set.Remove(item);
        return item;
    }

    public T Peek() => queue.Peek();

    public bool Contains(T item) => set.Contains(item);

    public void Clear()
    {
        queue.Clear();
        set.Clear();
    }

    public void CopyTo(Array array, int index)
    {
        queue.CopyTo((T[])array, index);
    }
    public IEnumerator GetEnumerator()
    {
        return queue.GetEnumerator();
    }
}
