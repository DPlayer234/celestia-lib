using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.Collections;
using CelestiaCS.Lib.Collections.Internal;

namespace CelestiaCS.Lib.Linq;

/// <summary>
/// Provides extension methods for list types.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Determines the index of the <paramref name="item"/> in the <paramref name="list"/> based on the default equality comparer.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="list"> The list to search. </param>
    /// <param name="item"> The item to search for. </param>
    /// <returns> The index of the item, or <c>-1</c> if it is not contained. </returns>
    public static int IndexOf<T>(this IReadOnlyList<T> list, T item)
    {
        if (list is IList<T> mutList)
        {
            return mutList.IndexOf(item);
        }

        int count = list.Count;
        if (typeof(T).IsValueType)
        {
            for (int i = 0; i < count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(list[i], item))
                    return i;
            }
        }
        else
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < count; i++)
            {
                if (comparer.Equals(list[i], item))
                    return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Determines the index of the <paramref name="item"/> in the <paramref name="list"/> based on the provided equality comparer.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="list"> The list to search. </param>
    /// <param name="item"> The item to search for. </param>
    /// <param name="comparer"> The equality comparer to use. </param>
    /// <returns> The index of the item, or <c>-1</c> if it is not contained. </returns>
    public static int IndexOf<T>(this IReadOnlyList<T> list, T item, IEqualityComparer<T>? comparer)
    {
        if (comparer == null || comparer == EqualityComparer<T>.Default)
        {
            return IndexOf(list, item);
        }

        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            if (comparer.Equals(list[i], item))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the index of an item matching the <paramref name="predicate"/> in the <paramref name="list"/>.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="list"> The list to search. </param>
    /// <param name="predicate"> The predicate to match items with. </param>
    /// <returns> The index of the first found item, or <c>-1</c> if it is not contained. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="predicate"/> is null. </exception>
    public static int FindIndex<T>(this IReadOnlyList<T> list, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(predicate);

        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            if (predicate(list[i]))
                return i;
        }

        return -1;
    }

    /// <inheritdoc cref="FindIndex{T}(IReadOnlyList{T}, Func{T, bool})"/>
    public static int FindIndex<T>(this IList<T> list, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(predicate);

        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            if (predicate(list[i]))
                return i;
        }

        return -1;
    }

    #region FindBy

    /// <summary>
    /// Finds an item in a collection by a key selector.
    /// </summary>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <typeparam name="TKey"> The type of the value to find it by. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="key"> The key to look for. </param>
    /// <param name="selector"> The key selector. </param>
    /// <returns> The found item, or default. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="selector"/> is null. </exception>
    public static T? FindBy<T, TKey>(this IEnumerable<T> source, TKey key, Func<T, TKey> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var enumerator = source.GetEnumerator();
        return FindByCore(new InverseStructEnumerator<T, IEnumerator<T>>(enumerator), key, selector);
    }

    /// <inheritdoc cref="FindBy{T, TKey}(IEnumerable{T}, TKey, Func{T, TKey})"/>
    public static T? FindBy<T, TKey>(this T[] source, TKey key, Func<T, TKey> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        return FindByCore(new ArrayEnumerator<T>(source), key, selector);
    }

    /// <inheritdoc cref="FindBy{T, TKey}(IEnumerable{T}, TKey, Func{T, TKey})"/>
    public static T? FindBy<T, TKey>(this ImmutableArray<T> source, TKey key, Func<T, TKey> selector)
    {
        return FindBy(ImmutableCollectionsMarshal.AsArray(source)!, key, selector);
    }

    private static T? FindByCore<T, TKey, TEnumerator>(TEnumerator enumerator, TKey key, Func<T, TKey> selector)
        where TEnumerator : IStructEnumerator<T>
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (typeof(TKey).IsValueType)
        {
            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                if (EqualityComparer<TKey>.Default.Equals(selector(item), key))
                    return item;
            }
        }
        else
        {
            var comparer = EqualityComparer<TKey>.Default;
            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                if (comparer.Equals(selector(item), key))
                    return item;
            }
        }

        return default;
    }

    #endregion

    /// <summary>
    /// Tries to get an item at the specified index. Fails without an exception when indexing out-of-bounds.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="list"> The list of items. </param>
    /// <param name="index"> The index to get an item at. </param>
    /// <param name="value"> The value that was at that index. </param>
    /// <returns> If an item was at that index. </returns>
    public static bool TryGetAt<T>(this IReadOnlyList<T> list, int index, [MaybeNullWhen(false)] out T value)
    {
        if ((uint)index < (uint)list.Count)
        {
            value = list[index];
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc cref="TryGetAt{T}(IReadOnlyList{T}, int, out T)"/>
    public static bool TryGetAt<T>(this T[] list, int index, [MaybeNullWhen(false)] out T value)
    {
        if ((uint)index < (uint)list.Length)
        {
            value = list[index];
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc cref="TryGetAt{T}(IReadOnlyList{T}, int, out T)"/>
    public static bool TryGetAt<T>(this ImmutableArray<T> list, int index, [MaybeNullWhen(false)] out T value)
    {
        return TryGetAt(ImmutableCollectionsMarshal.AsArray(list)!, index, out value);
    }

    /// <inheritdoc cref="TryGetAt{T}(IReadOnlyList{T}, int, out T)"/>
    /// <remarks>
    /// This method has a suffix to avoid ambiguity for types that implement
    /// both the mutable and the read-only list interfaces, which is very common.
    /// </remarks>
    public static bool TryGetAt_<T>(this IList<T> list, int index, [MaybeNullWhen(false)] out T value)
    {
        if ((uint)index < (uint)list.Count)
        {
            value = list[index];
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Allows enumerating over a slice of the list, from index <paramref name="start"/>, for <paramref name="length"/> items.
    /// It's valid to have less than <paramref name="length"/> items available.
    /// </summary>
    /// <remarks>
    /// This method is primarily intended for pagination of lists and similar mechanism.
    /// </remarks>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="list"> The list to slice. </param>
    /// <param name="start"> The start index. </param>
    /// <param name="length"> The amount of items to include. </param>
    /// <returns> A slice enumerable. </returns>
    public static SoftSliceEnumerable<T> SoftSlice<T>(this IReadOnlyList<T> list, int start, int length)
    {
        ArgumentNullException.ThrowIfNull(list);
        return new SoftSliceEnumerable<T>(list, start, length);
    }

    /// <summary>
    /// Allows enumerating over a slice of the list with a given range.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="list"> The list to slice. </param>
    /// <param name="range"> The range to slice. </param>
    /// <returns> A slice enumerable. </returns>
    public static SoftSliceEnumerable<T> SoftSlice<T>(this IReadOnlyList<T> list, Range range)
    {
        ArgumentNullException.ThrowIfNull(list);
        var (start, length) = range.GetOffsetAndLength(list.Count);
        return new SoftSliceEnumerable<T>(list, start, length);
    }

    /// <summary>
    /// Unzips a collection of 2-element tuples into 2 arrays.
    /// </summary>
    /// <typeparam name="TLeft"> The type of the first element. </typeparam>
    /// <typeparam name="TRight"> The type of the second element. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <returns> The split arrays. </returns>
    public static (TLeft[], TRight[]) UnzipToArrays<TLeft, TRight>(this IEnumerable<(TLeft, TRight)> source)
    {
        ValueList<TLeft> aRes = default;
        ValueList<TRight> bRes = default;

        if (source.TryGetNonEnumeratedCount(out int count))
        {
            aRes.EnsureCapacity(count);
            bRes.EnsureCapacity(count);
        }

        foreach (var (a, b) in source)
        {
            aRes.Add(a);
            bRes.Add(b);
        }

        return (aRes.DrainToArray(), bRes.DrainToArray());
    }

    /// <summary>
    /// If the <paramref name="array"/> is default, returns <see cref="ImmutableArray{T}.Empty"/>. Otherwise returns the <paramref name="array"/>.
    /// </summary>
    /// <typeparam name="T"> The type of the array elements. </typeparam>
    /// <param name="array"> The array. </param>
    /// <returns> The same array or <see cref="ImmutableArray{T}.Empty"/> if it is default. </returns>
    public static ImmutableArray<T> OrEmpty<T>(this ImmutableArray<T> array)
    {
        if (array.IsDefault) return [];
        return array;
    }
}

/// <summary>
/// Represents a soft slice over a collection. This type is primarily intended to be enumerated.
/// </summary>
/// <typeparam name="T"> The type of items. </typeparam>
public readonly struct SoftSliceEnumerable<T> : IEnumerable<T>
{
    private readonly Enumerator _inner;

    internal SoftSliceEnumerable(IReadOnlyList<T> list, int start, int length)
    {
        _inner = new Enumerator(list, start, length);
    }

    public Enumerator GetEnumerator()
        => _inner;

    /// <summary>
    /// Copies the slice contents to an array.
    /// </summary>
    /// <returns> The slice as an array. </returns>
    public T[] ToArray()
    {
        var list = _inner.list;
        int start = _inner.index + 1;
        int end = _inner.end;
        int count = end - start;

        T[] result = new T[count];
        for (int i = 0; i < count; i++) result[i] = list[i + start];
        return result;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => new StructEnumerator<T, Enumerator>(_inner);

    IEnumerator IEnumerable.GetEnumerator()
        => new StructEnumerator<T, Enumerator>(_inner);

    public struct Enumerator : IStructEnumerator<T>
    {
        internal readonly IReadOnlyList<T> list;
        internal readonly int end;
        internal int index;

        public Enumerator(IReadOnlyList<T> list, int start, int length)
        {
            this.list = list;
            index = start - 1;

            int end = start + length;
            this.end = end < this.list.Count ? end : this.list.Count;
        }

        public readonly T Current => list[index];

        public bool MoveNext()
        {
            return ++index < end;
        }
    }
}
