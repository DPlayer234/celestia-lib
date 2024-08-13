using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.Collections;

namespace CelestiaCS.Lib.Dangerous;

/// <summary>
/// Provides unsafe methods to work with collections.
/// </summary>
public static class DangerousCollection
{
    /// <summary>
    /// Gets the backing collection for a <see cref="ReadOnlyCollectionBase{TItem, TCollection}"/> derived type.
    /// </summary>
    /// <remarks>
    /// There are no type or memory safety concerns when calling.
    /// This method is only dangerous because it allows violating the exposed-as-read-only semantics.
    /// The caller must ensure that the invariants are not violated.
    /// </remarks>
    /// <typeparam name="TItem"> The type of elements. </typeparam>
    /// <typeparam name="TCollection"> The backing collection type. </typeparam>
    /// <param name="collection"> The read-only collection. </param>
    /// <returns> The backing collection. </returns>
    public static TCollection GetCollection<TItem, TCollection>(ReadOnlyCollectionBase<TItem, TCollection> collection) where TCollection : class, ICollection<TItem>
    {
        return collection.UnsafeGetCollection();
    }

    /// <summary>
    /// Gets the backing collection for a <see cref="ReadOnlyList{T}"/> as an <see cref="ImmutableArray{T}"/>.
    /// </summary>
    /// <remarks>
    /// There are no type or memory safety concerns when calling.
    /// This is only unsafe because the backing collection might be held by other objects and treated as mutable.
    /// The caller must ensure that the invariants of <see cref="ImmutableArray{T}"/> are not violated.
    /// </remarks>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="list"> The read-only list. </param>
    /// <returns> The backing collection. </returns>
    public static ImmutableArray<T> AsImmutableArray<T>(ReadOnlyList<T> list)
    {
        return ImmutableCollectionsMarshal.AsImmutableArray(list.UnsafeGetCollection());
    }

    /// <summary>
    /// Gets the backing array for a value-list.
    /// </summary>
    /// <remarks>
    /// The guidance that applies to <seealso cref="ValueList{T}.AsSpan"/> applies, applies to this method too.
    /// </remarks>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="list"> The list. </param>
    /// <returns> The current backing array. </returns>
    public static T[]? GetArray<T>(ValueList<T> list)
    {
        return list.WithAllocator()._array;
    }

    /// <inheritdoc cref="GetArray{T}(ValueList{T})"/>
    /// <typeparam name="TAlloc"> The used allocator type. </typeparam>
    public static T[]? GetArray<T, TAlloc>(ValueList<T, TAlloc> list) where TAlloc : IArrayAllocator
    {
        return list._array;
    }
}
