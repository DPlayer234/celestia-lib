using System;
using System.Collections.Generic;
using System.Linq;

namespace CelestiaCS.Lib.Linq;

partial class EnumerableExtensions
{
    /// <summary>
    /// Determines if the <paramref name="source"/> collection has <paramref name="count"/> or more items in an efficient manner.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="count"> The amount of items to check for. </param>
    /// <returns> Whether the <paramref name="source"/> has at least <paramref name="count"/> items. </returns>
    public static bool HasAtLeast<T>(this IEnumerable<T> source, int count)
    {
        return CountUpTo(source, count) >= count;
    }

    /// <summary>
    /// Determines if the <paramref name="source"/> collection has <paramref name="count"/> or less items in an efficient manner.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="count"> The amount of items to check for. </param>
    /// <returns> Whether the <paramref name="source"/> has at most <paramref name="count"/> items. </returns>
    public static bool HasAtMost<T>(this IEnumerable<T> source, int count)
    {
        return CountUpTo(source, count + 1) <= count;
    }

    /// <summary>
    /// Determines if the <paramref name="source"/> collection has exactly <paramref name="count"/> items in an efficient manner.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="count"> The amount of items to check for. </param>
    /// <returns> Whether the <paramref name="source"/> has exactly <paramref name="count"/> items. </returns>
    public static bool HasExactly<T>(this IEnumerable<T> source, int count)
    {
        return CountUpTo(source, count + 1) == count;
    }

    /// <summary>
    /// Counts the items in the <paramref name="source"/> up to the specified <paramref name="maxCount"/>.
    /// </summary>
    /// <remarks>
    /// This is done either using the non-enumerated count or by enumerating until the counter reaches that point.
    /// If the non-enumerated count can be used, this may return a value higher than <paramref name="maxCount"/>.
    /// </remarks>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="maxCount"> How far to count. </param>
    /// <returns> The count. </returns>
    public static int CountUpTo<T>(this IEnumerable<T> source, int maxCount)
    {
        if (maxCount <= 0)
        {
            ArgumentNullException.ThrowIfNull(source);
            return 0;
        }

        if (source.TryGetNonEnumeratedCount(out int count))
            return count;

        count = 0;
        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext() && count < maxCount)
            {
                checked { count += 1; }
            }
        }

        return count;
    }

    /// <inheritdoc cref="HasAtLeast{T}(IEnumerable{T}, int)"/>
    /// <param name="where"> A filter on the items to count. </param>
    public static bool HasAtLeast<T>(this IEnumerable<T> source, int count, Func<T, bool> where)
    {
        return CountUpTo(source, count, where) >= count;
    }

    /// <inheritdoc cref="HasAtMost{T}(IEnumerable{T}, int)"/>
    /// <param name="where"> A filter on the items to count. </param>
    public static bool HasAtMost<T>(this IEnumerable<T> source, int count, Func<T, bool> where)
    {
        return CountUpTo(source, count + 1, where) <= count;
    }

    /// <inheritdoc cref="HasExactly{T}(IEnumerable{T}, int)"/>
    /// <param name="where"> A filter on the items to count. </param>
    public static bool HasExactly<T>(this IEnumerable<T> source, int count, Func<T, bool> where)
    {
        return CountUpTo(source, count + 1, where) == count;
    }

    /// <inheritdoc cref="CountUpTo{T}(IEnumerable{T}, int)"/>
    /// <param name="where"> A filter on the items to count. </param>
    public static int CountUpTo<T>(this IEnumerable<T> source, int maxCount, Func<T, bool> where)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(where);

        if (maxCount <= 0)
            return 0;

        int count = 0;
        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext() && count < maxCount)
            {
                if (where(enumerator.Current))
                    checked { count += 1; }
            }
        }

        return count;
    }
}
