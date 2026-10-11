using System;

namespace Luminix;

public abstract class ObjectPool<T> where T : class, new()
{
    private protected const int DefaultMaxRetained = 256;

    private protected ObjectPool(Action<T> reset, int maxRetained)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxRetained, 0, nameof(maxRetained));

        Reset = reset;
        MaxRetained = maxRetained;
    }

    public static ObjectPool<T> Shared { get; } = new ConcurrentObjectPool<T>(null, DefaultMaxRetained);

    public static ObjectPool<T> Create(Action<T> reset = null, int maxRetained = DefaultMaxRetained)
        => new LocalObjectPool<T>(reset, maxRetained);

    public static ObjectPool<T> CreateConcurrent(Action<T> reset = null, int maxRetained = DefaultMaxRetained)
        => new ConcurrentObjectPool<T>(reset, maxRetained);

    private protected Action<T> Reset { get; }
    private protected int MaxRetained { get; }

    public abstract T Rent();
    public abstract void Return(T item);
}