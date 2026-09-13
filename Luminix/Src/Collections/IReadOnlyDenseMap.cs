using System.Collections.Generic;

namespace Luminix.Collections;

public interface IReadOnlyDenseMap<T1, T2> : IEnumerable<(T1, T2)>, IReadOnlyCollection<(T1, T2)> where T1 : notnull
{
    (T1, T2) this[int index] { get; }
    T2 this[T1 left] { get; }
    T1 this[T2 right] { get; }

    DenseMap<T1, T2>.LeftEnumerable LeftItems { get; }
    DenseMap<T1, T2>.RightEnumerable RightItems { get; }

    bool ContainsItem(T1 left);
    bool ContainsItem(T2 right);
    bool TryGetItem(T2 right, out T1 left);
    bool TryGetItem(T1 left, out T2 right);

    bool TryGetItemIndex(T1 left, out int index);
    bool TryGetItemIndex(T2 right, out int index);
}
