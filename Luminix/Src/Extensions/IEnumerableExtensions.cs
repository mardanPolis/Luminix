using System;
using System.Collections.Generic;
using Luminix.Collections;

namespace Luminix.Extensions;

public static class IEnumerableExtensions
{
    public static DenseList<T> ToDenseList<T>(this IEnumerable<T> source)
    {
        return source is null ? throw new ArgumentNullException(nameof(source)) : [.. source];
    }
}