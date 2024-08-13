using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using CelestiaCS.Lib.Collections.Internal;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// Provides a collection wrapping read-only collection base class.
/// </summary>
/// <remarks>
/// DO NOT use this type directly. Instead, use the derived types.
/// </remarks>
/// <typeparam name="TItem"> The type of the items. </typeparam>
/// <typeparam name="TCollection"> The type of the wrapped collection. </typeparam>
public abstract class ReadOnlyCollectionBase<TItem, TCollection>
    : IReadOnlyCollection<TItem>
    , ICollection<TItem>
    , ICollection
    where TCollection : class, ICollection<TItem>
{
    /// <summary> The wrapped inner collection. </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    protected readonly TCollection Inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyCollectionBase{TItem, TCollection}"/> class by wrapping a collection.
    /// The collection is not copied.
    /// </summary>
    /// <param name="collection"> The collection to wrap. </param>
    private protected ReadOnlyCollectionBase(TCollection collection)
    {
        Inner = collection;
    }

    // This property is abstract so that inheriting classes can override it
    // instead of causing a virtual call through ICollection<T>
    /// <inheritdoc/>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public abstract int Count { get; }

    /// <summary> A reusable enumerator for this instance. By default, used for the empty case. </summary>
    protected IEnumerator<TItem>? ReusableEnumerator { get; init; }

    // The reasoning for this being abstract is the same as for Count.
    /// <summary>
    /// Determines whether the collection contains a specific value.
    /// </summary>
    /// <param name="item"> The item to look for. </param>
    /// <returns> <see langword="true"/> if <paramref name="item"/> is found in the collection; otherwise, <see langword="false"/>. </returns>
    public abstract bool Contains(TItem item);

    // This method is not abstract because ReadOnlyList<T> cannot override it.
    /// <inheritdoc/>
    public virtual IEnumerator<TItem> GetEnumerator() => ReusableEnumerator ?? Inner.GetEnumerator();

    internal TCollection UnsafeGetCollection() => Inner;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #region ICollection<~> impl

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool ICollection<TItem>.IsReadOnly => true;

    void ICollection<TItem>.Add(TItem item) => throw new NotSupportedException();
    void ICollection<TItem>.Clear() => throw new NotSupportedException();
    bool ICollection<TItem>.Remove(TItem item) => throw new NotSupportedException();
    void ICollection<TItem>.CopyTo(TItem[] array, int arrayIndex) => Inner.CopyTo(array, arrayIndex);

    #endregion

    #region ICollection impl

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool ICollection.IsSynchronized => false;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    object ICollection.SyncRoot => this;

    void ICollection.CopyTo(Array array, int index)
    {
        var inner = Inner;
        if (inner is ICollection coll)
        {
            coll.CopyTo(array, index);
        }
        else if (array is TItem[] tArr)
        {
            inner.CopyTo(tArr, 0);
        }
        else
        {
            CollectionNGImplHelper.CopyTo(this, array, index);
        }
    }

    #endregion
}
