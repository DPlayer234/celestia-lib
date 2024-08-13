using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.Collections;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.Linq;

partial class EnumerableExtensions
{
    /// <summary>
    /// If the <paramref name="source"/> collection has exactly 1 item, returns that item.
    /// Otherwise, returns <see langword="default"/>.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <returns> The only item in the collection or <see langword="default"/>. </returns>
    public static T? AsSingle<T>(this IEnumerable<T> source)
    {
        if (source is IList<T> list)
        {
            if (list.Count == 1)
                return list[0];

            return default;
        }

        return Fallback(source);

        static T? Fallback(IEnumerable<T> source)
        {
            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                return default; // Empty enumerable

            // 1st Item in collection
            var result = enumerator.Current;

            if (enumerator.MoveNext())
                return default; // More than 1 item

            return result;
        }
    }

    /// <summary>
    /// If the <paramref name="source"/> collection has exactly 1 item, returns <see langword="true"/> and sets <paramref name="result"/> to it.
    /// Otherwise, returns <see langword="false"/>.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="result"> The only item in the collection or <see langword="default"/>. </param>
    /// <returns> Whether the <paramref name="source"/> collection has exactly 1 item. </returns>
    public static bool TryAsSingle<T>(this IEnumerable<T> source, [MaybeNullWhen(false)] out T result)
    {
        if (source is IList<T> list)
        {
            if (list.Count == 1)
            {
                result = list[0];
                return true;
            }

            result = default;
            return false;
        }

        return Fallback(source, out result);

        static bool Fallback(IEnumerable<T> source, [MaybeNullWhen(false)] out T result)
        {
            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                goto Fail; // Empty enumerable

            // 1st Item in collection
            result = enumerator.Current;

            if (enumerator.MoveNext())
                goto Fail; // More than 1 item

            return true;

        Fail:
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Copies the source enumerable to a pooled, disposable buffer.
    /// This is useful to not iterate the source buffer anymore while avoiding most allocations.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <returns> The pooled, disposable buffer. </returns>
    public static PooledBuffer<T> ToPooledBuffer<T>(this IEnumerable<T> source)
    {
        return PooledBuffer<T>.From(source);
    }

    /// <summary>
    /// Copies the contents of the <paramref name="source"/> collection to a new <see cref="ValueList{T}"/>.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <returns> A <see cref="ValueList{T}"/> with the contents of the <paramref name="source"/>. </returns>
    public static ValueList<T> ToValueList<T>(this IEnumerable<T> source)
    {
        ValueList<T> result = default;
        result.AddRange(source);
        return result;
    }

    #region ConcatToArray

    /// <summary>
    /// Concatenates lists, selected from <paramref name="source"/>, and concatenates them into an array.
    /// </summary>
    /// <typeparam name="TSource"> The source element type. </typeparam>
    /// <typeparam name="TResult"> The result element type. </typeparam>
    /// <param name="source"> The source list. </param>
    /// <param name="selector"> The selector to get the lists to actually join. </param>
    /// <returns> An array with all the content of all selected lists concatenated. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="selector"/> is null. </exception>
    public static TResult[] ConcatToArray<TSource, TResult>(this IReadOnlyList<TSource> source, Func<TSource, IReadOnlyList<TResult>> selector)
    {
        return ConcatToArray<TSource, TResult, IReadOnlyList<TResult>, ConcatReadOnlyListHelper<TResult>>(source, selector);
    }

    /// <inheritdoc cref="ConcatToArray{TSource, TResult}(IReadOnlyList{TSource}, Func{TSource, IReadOnlyList{TResult}})"/>
    public static TResult[] ConcatToArray<TSource, TResult>(this IReadOnlyList<TSource> source, Func<TSource, ImmutableArray<TResult>> selector)
    {
        return ConcatToArray<TSource, TResult, ImmutableArray<TResult>, ConcatImmutableArrayHelper<TResult>>(source, selector);
    }

    /// <inheritdoc cref="ConcatToArray{TSource, TResult}(IReadOnlyList{TSource}, Func{TSource, IReadOnlyList{TResult}})"/>
    public static TResult[] ConcatToArray<TSource, TResult>(this ImmutableArray<TSource> source, Func<TSource, IReadOnlyList<TResult>> selector)
    {
        return ImmutableCollectionsMarshal.AsArray(source)!.ConcatToArray(selector);
    }

    /// <inheritdoc cref="ConcatToArray{TSource, TResult}(IReadOnlyList{TSource}, Func{TSource, IReadOnlyList{TResult}})"/>
    public static TResult[] ConcatToArray<TSource, TResult>(this ImmutableArray<TSource> source, Func<TSource, ImmutableArray<TResult>> selector)
    {
        return ImmutableCollectionsMarshal.AsArray(source)!.ConcatToArray(selector);
    }

    private static TResult[] ConcatToArray<TSource, TResult, TCollection, THelper>(this IReadOnlyList<TSource> source, Func<TSource, TCollection> selector)
        where THelper : IConcatHelper<TResult, TCollection>
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        int count = source.Count;
        if (count == 0) goto Empty;

        InlineArray8<TCollection> buffer = default;
        Span<TCollection> temp = count <= 8 ? buffer[..count] : new TCollection[count];
        int totalLen = 0;

        for (int i = 0; i < temp.Length; i++)
        {
            var list = selector(source[i]);
            temp[i] = list;
            totalLen += THelper.GetCount(list);
        }

        if (totalLen == 0) goto Empty;

        TResult[] result = new TResult[totalLen];
        int start = 0;

        for (int i = 0; i < temp.Length; i++)
        {
            var list = temp[i];
            THelper.CopyTo(list, result, start);
            start += THelper.GetCount(list);
        }

        Debug.Assert(start == result.Length);
        return result;

    Empty:
        return [];
    }

    private static void ReadOnlyCopyTo<T>(IReadOnlyList<T> source, T[] array, int index)
    {
        if (source is ICollection<T> collection)
        {
            collection.CopyTo(array, index);
        }
        else
        {
            int count = source.Count;
            for (int i = 0; i < count; i++)
            {
                array[index++] = source[i];
            }
        }
    }

    private interface IConcatHelper<TResult, TCollection>
    {
        static abstract int GetCount(TCollection collection);
        static abstract void CopyTo(TCollection source, TResult[] array, int index);
    }

    private readonly struct ConcatReadOnlyListHelper<TResult> : IConcatHelper<TResult, IReadOnlyList<TResult>>
    {
        public static int GetCount(IReadOnlyList<TResult> collection) => collection.Count;
        public static void CopyTo(IReadOnlyList<TResult> source, TResult[] array, int index) => ReadOnlyCopyTo(source, array, index);
    }

    private readonly struct ConcatImmutableArrayHelper<TResult> : IConcatHelper<TResult, ImmutableArray<TResult>>
    {
        public static int GetCount(ImmutableArray<TResult> collection) => collection.Length;
        public static void CopyTo(ImmutableArray<TResult> source, TResult[] array, int index) => source.CopyTo(array, index);
    }

    #endregion
}
