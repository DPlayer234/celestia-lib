using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib.Linq;

/// <summary>
/// Provides general extensions for enumerables.
/// </summary>
public static partial class EnumerableExtensions
{
    /// <summary>
    /// Returns the first item of type <typeparamref name="T"/> in the <paramref name="source"/> collection.
    /// </summary>
    /// <typeparam name="T"> The type of item to search for. </typeparam>
    /// <param name="source"> The collection to search. </param>
    /// <returns> The first found item, or <see langword="null"/> if none was found. </returns>
    public static T? FirstOfType<T>(this IEnumerable<object?> source)
        where T : class
    {
        foreach (var item in source)
        {
            // This is somehow *much* faster:
            // Apparently 'item is T child' inserts an unneeded
            // ChkCastAny *after* an IsInstanceOfAny here.
            if (item is T)
            {
                return Unsafe.As<T>(item);
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if any item in the <paramref name="source"/> collection is of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"> The type of item to search for. </typeparam>
    /// <param name="source"> The collection to search. </param>
    /// <returns> If any item is of type <typeparamref name="T"/>. </returns>
    public static bool AnyOfType<T>(this IEnumerable<object?> source)
        where T : class
    {
        foreach (var item in source)
        {
            if (item is T)
            {
                return true;
            }
        }

        return false;
    }

    #region Overloads to serve warnings

    /// <inheritdoc cref="AnyOfType{T}(IEnumerable{object?})"/>
    [Obsolete("The type you check for is the type of the collection or a super-type of it. This statement will always return Any().")]
    public static bool AnyOfType<T>(this IEnumerable<T?> source) where T : class => source.Any();

    /// <inheritdoc cref="FirstOfType{T}(IEnumerable{object?})"/>
    [Obsolete("The type you check for is the type of the collection or a super-type of it. This statement will always return the first element.")]
    public static T? FirstOfType<T>(this IEnumerable<T?> source) where T : class => source.FirstOrDefault();

    #endregion

    /// <summary>
    /// Enumerates the source sequence but excludes the specified item.
    /// </summary>
    /// <remarks>
    /// Reference types use reference equality.
    /// Otherwise, <seealso cref="EqualityComparer{T}.Default"/> is used.
    /// </remarks>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="item"> The item to exclude. </param>
    /// <returns> A new enumerable without the <paramref name="item"/>. </returns>
    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T? item)
    {
        if (typeof(T).IsValueType)
            return source.ExceptByValue(item);
        else
            return ForClass(source, item);

        static IEnumerable<T> ForClass(IEnumerable<T> source, T? item)
        {
            foreach (var element in source)
            {
                if (!ReferenceEquals(element, item))
                    yield return element;
            }
        }
    }

    /// <summary>
    /// Enumerates the source sequence but excludes the specified item.
    /// </summary>
    /// <remarks>
    /// <seealso cref="EqualityComparer{T}.Default"/> is used for equality.
    /// </remarks>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="item"> The item to exclude. </param>
    /// <returns> A new enumerable without the <paramref name="item"/>. </returns>
    public static IEnumerable<T> ExceptByValue<T>(this IEnumerable<T> source, T? item)
    {
        foreach (var element in source)
        {
            if (!EqualityComparer<T>.Default.Equals(element, item))
                yield return element;
        }
    }

    /// <summary>
    /// Enumerates the source sequence but excludes the specified item.
    /// </summary>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="item"> The item to exclude. </param>
    /// <param name="equalityComparer"> The equality comparer to use. </param>
    /// <returns> A new enumerable without the <paramref name="item"/>. </returns>
    public static IEnumerable<T> ExceptByValue<T>(this IEnumerable<T> source, T? item, IEqualityComparer<T>? equalityComparer)
    {
        return equalityComparer is null
            ? ExceptByValue(source, item)
            : Enumerator(source, item, equalityComparer);

        static IEnumerable<T> Enumerator(IEnumerable<T> source, T? item, IEqualityComparer<T> equalityComparer)
        {
            foreach (var element in source)
            {
                if (!equalityComparer.Equals(element, item))
                    yield return element;
            }
        }
    }

    /// <summary>
    /// Filters a sequence of elements by removing <see langword="null"/> values from it.
    /// </summary>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <returns> The collection without <see langword="null"/> values. </returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
    {
        return source.Where(t => t != null)!;
    }

    /// <summary>
    /// Filters a sequence of nullable value-type elements by removing <see langword="null"/> values from it.
    /// </summary>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <returns> The collection without <see langword="null"/> values. </returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
    {
        return source.Where(t => t != null).Select(t => t.GetValueOrDefault());
    }

    /// <summary>
    /// Allows enumerating an <see cref="IEnumerator{T}"/> from its current state via the <see langword="foreach"/> construct.
    /// </summary>
    /// <remarks>
    /// The returned type does not implement <see cref="IDisposable"/>: Manual disposal or a using statement is needed.
    /// </remarks>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="enumerator"> The enumerator to run. </param>
    /// <returns> An enumerable enumerator over the argument. </returns>
    public static EnumerableEnumerator<T> Enumerate<T>(this IEnumerator<T> enumerator)
    {
        return new(enumerator);
    }

    public readonly struct EnumerableEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;

        public EnumerableEnumerator(IEnumerator<T> enumerator) => _enumerator = enumerator;
        public EnumerableEnumerator<T> GetEnumerator() => this;

        public T Current => _enumerator.Current;
        public bool MoveNext() => _enumerator.MoveNext();
    }
}
