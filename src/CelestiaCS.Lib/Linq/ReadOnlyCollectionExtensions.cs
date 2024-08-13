using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CelestiaCS.Lib.Collections;

namespace CelestiaCS.Lib.Linq;

/// <summary>
/// Provides extension methods to create read-only collections.
/// </summary>
public static class ReadOnlyCollectionExtensions
{
    /// <summary>
    /// Creates a read-only list from an enumerable.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source enumerable. </param>
    /// <returns> A read-only list with the items of the enumerable. </returns>
    public static ReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source)
    {
        return source switch
        {
            ImmutableArray<T> immutable => ReadOnlyList.Create(immutable),
            _ => ReadOnlyList.Create(source.ToArray())
        };
    }

    /// <summary>
    /// Creates a read-only dictionary from an enumerable, by selecting a key and using the items as the values.
    /// </summary>
    /// <typeparam name="TKey"> The key type. </typeparam>
    /// <typeparam name="TValue"> The value type. </typeparam>
    /// <param name="source"> The source enumerable. </param>
    /// <param name="keySelector"> A function that will select a key from the items. </param>
    /// <param name="keyComparer"> The key comparer to use. </param>
    /// <returns> A read-only dictionary with the mapped items of the enumerable. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="keySelector"/> is null. </exception>
    public static ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IEnumerable<TValue> source, Func<TValue, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null)
        where TKey : notnull
        => ReadOnlyDictionary.Create(source.ToDictionary(keySelector, keyComparer));

    /// <summary>
    /// Creates a read-only dictionary from an enumerable, by selecting a key and a value.
    /// </summary>
    /// <typeparam name="TKey"> The key type. </typeparam>
    /// <typeparam name="TValue"> The value type. </typeparam>
    /// <param name="source"> The source enumerable. </param>
    /// <param name="keySelector"> A function that will select a key from the items. </param>
    /// <param name="elementSelector"> A function that will select a value from the items. </param>
    /// <param name="keyComparer"> The key comparer to use. </param>
    /// <returns> A read-only dictionary with the mapped items of the enumerable. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="keySelector"/> or <paramref name="elementSelector"/> is null. </exception>
    public static ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IEqualityComparer<TKey>? keyComparer = null)
        where TKey : notnull
        => ReadOnlyDictionary.Create(source.ToDictionary(keySelector, elementSelector, keyComparer));

    /// <summary>
    /// Creates a read-only dictionary from an enumerable of <see cref="KeyValuePair{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey"> The key type. </typeparam>
    /// <typeparam name="TValue"> The value type. </typeparam>
    /// <param name="source"> The source enumerable. </param>
    /// <param name="keyComparer"> The key comparer to use. </param>
    /// <returns> A read-only dictionary with the items of the enumerable. </returns>
    public static ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? keyComparer = null)
        where TKey : notnull
        => ReadOnlyDictionary.Create(source.ToDictionary(keyComparer));

    /// <summary>
    /// Creates a read-only set from an enumerable.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source enumerable. </param>
    /// <param name="comparer"> The item equality comparer to use. </param>
    /// <returns> A read-only set with the items of the enumerable. </returns>
    public static ReadOnlySet<T> ToReadOnlySet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null)
        => ReadOnlySet.Create(source.ToHashSet(comparer));

    #region For ValueList<T>

    /// <inheritdoc cref="ToReadOnlyList{T}(IEnumerable{T})"/>
    public static ReadOnlyList<T> ToReadOnlyList<T>(this in ValueList<T> source)
        => ReadOnlyList.Create(source.ToArray());

    /// <inheritdoc cref="ToReadOnlyList{T}(IEnumerable{T})"/>
    /// <typeparam name="TAlloc"> The list's allocator. </typeparam>
    public static ReadOnlyList<T> ToReadOnlyList<T, TAlloc>(this in ValueList<T, TAlloc> source) where TAlloc : IArrayAllocator
        => ReadOnlyList.Create(source.ToArray());

    #endregion
}
