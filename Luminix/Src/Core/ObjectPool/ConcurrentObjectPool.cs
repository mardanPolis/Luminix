using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Luminix;

internal class ConcurrentObjectPool<T> : ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentStack<T> items = new();
    private int count;

    internal ConcurrentObjectPool(Action<T> reset, int maxRetained) : base(reset, maxRetained) { }

    public override T Rent()
    {
        if (items.TryPop(out T item))
        {
            Interlocked.Decrement(ref count);
            return item;
        }
        return new T();
    }

    public override void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Reset?.Invoke(item);

        if (Interlocked.Increment(ref count) > MaxRetained)
        {
            Interlocked.Decrement(ref count);
            return;
        }

        items.Push(item);
    }
}
