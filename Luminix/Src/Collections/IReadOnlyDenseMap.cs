using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Luminix.Collections;

public interface IReadOnlyDenseMap<T1, T2> : IEnumerable<(T1, T2)>, IReadOnlyCollection<(T1, T2)> where T1 : notnull
{
    T2 this[T1 left] { get; }
    T1 this[T2 right] { get; }

    DenseMap<T1, T2>.LeftEnumerable LeftItems { get; }
    DenseMap<T1, T2>.RightEnumerable RightItems { get; }

    bool ContainsLeft(T1 key);
    bool ContainsRight(T2 key);
    bool TryGetLeft(T2 key, out T1 value);
    bool TryGetRight(T1 key, out T2 value);
}
