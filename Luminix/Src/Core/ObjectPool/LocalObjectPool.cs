using System;
using System.Collections.Generic;

namespace Luminix;

internal sealed class LocalObjectPool<T> : ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> items = new();

    internal LocalObjectPool(Action<T> reset, int maxRetained) : base(reset, maxRetained) { }

    public override T Rent() => items.Count > 0 ? items.Pop() : new T();

    public override void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Reset?.Invoke(item);

        if (items.Count < MaxRetained)
            items.Push(item);
    }
}