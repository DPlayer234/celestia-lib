using System.Collections.Generic;
using System.Linq;

namespace CelestiaCS.Lib.Linq;

/// <summary>
/// Provides additional static method to work with or create enumerables.
/// </summary>
public static class EnumerableEx
{
    /// <summary>
    /// Generates a sequence that holds just a single value.
    /// </summary>
    /// <typeparam name="T"> The type of the item. </typeparam>
    /// <param name="value"> The value to yield. </param>
    /// <returns> An <see cref="IEnumerable{T}"/> that contains the value. </returns>
    public static IEnumerable<T> Once<T>(T value) => Enumerable.Repeat(value, 1);
}
