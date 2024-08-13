using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib.Linq;

partial class EnumerableExtensions
{
    /// <summary>
    /// Skips and take some items of a collection in one call.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="skip"> The amount of items to skip at the beginning. </param>
    /// <param name="take"> The maximum amount of items to take after skipping. </param>
    /// <returns> The taken items. </returns>
    public static IEnumerable<T> SkipTake<T>(this IEnumerable<T> source, int skip, int take)
    {
        return source.Take(skip..(skip + take));
    }

    /// <summary>
    /// Takes a page out of a collection.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="pageIndex"> The index of the page. (0-based!) </param>
    /// <param name="pageSize"> The maximum size of each page. </param>
    /// <returns> The page of the collection. </returns>
    public static IEnumerable<T> TakePage<T>(this IEnumerable<T> source, int pageIndex, int pageSize)
    {
        int start = pageIndex * pageSize;
        return source.SkipTake(start, pageSize);
    }

    /// <inheritdoc cref="TakePage{T}(IEnumerable{T}, int, int)"/>
    public static IEnumerable<T> TakePage<T>(this ImmutableArray<T> source, int pageIndex, int pageSize)
    {
        return TakePage(ImmutableCollectionsMarshal.AsArray(source)!, pageIndex, pageSize);
    }

    /// <summary>
    /// Takes a page out of a span.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source span. </param>
    /// <param name="pageIndex"> The index of the page. (0-based!) </param>
    /// <param name="pageSize"> The maximum size of each page. </param>
    /// <returns> The page of the collection. </returns>
    public static ReadOnlySpan<T> TakePage<T>(this ReadOnlySpan<T> source, int pageIndex, int pageSize)
    {
        int start = pageIndex * pageSize;
        return source.Slice(start, Math.Min(pageSize, source.Length - start));
    }

    /// <inheritdoc cref="TakePage{T}(ReadOnlySpan{T}, int, int)"/>
    public static Span<T> TakePage<T>(this Span<T> source, int pageIndex, int pageSize)
    {
        int start = pageIndex * pageSize;
        return source.Slice(start, Math.Min(pageSize, source.Length - start));
    }
}
