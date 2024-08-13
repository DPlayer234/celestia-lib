using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// Provides static helper method for <see cref="ReadOnlySet{T}"/>.
/// </summary>
public static class ReadOnlySet
{
    /// <summary>
    /// Creates a new <see cref="ReadOnlySet{T}"/> by wrapping the <paramref name="set"/>.
    /// If the <paramref name="set"/> is empty, returns <see cref="ReadOnlySet{T}.Empty"/> instead.
    /// The hash-set is never copied.
    /// </summary>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="set"> The set to wrap. </param>
    /// <returns> A read-only set wrapping the hash-set or equivalent. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="set"/> is null. </exception>
    public static ReadOnlySet<T> Create<T>(HashSet<T> set)
        => ReadOnlySet<T>.Create(set);
}

/// <summary>
/// A read-only set created by wrapping a hash-set.
/// </summary>
/// <typeparam name="T"> The type of the items. </typeparam>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class ReadOnlySet<T>
    : ReadOnlyCollectionBase<T, HashSet<T>>
    , IReadOnlySet<T>
    , ISet<T>
{
    /// <summary> The shared, empty set instance. </summary>
    public static ReadOnlySet<T> Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlySet{T}"/> class by wrapping a hash-set instance.
    /// The hash-set is not copied.
    /// </summary>
    /// <param name="set"> The hash-set to wrap. </param>
    private ReadOnlySet(HashSet<T> set) : base(set) { }

    /// <summary>
    /// Initializes an empty instance of the <see cref="ReadOnlySet{T}"/> class.
    /// </summary>
    private ReadOnlySet() : base([]) { ReusableEnumerator = NullImpl.EnumeratorOf<T>(); }

    /// <inheritdoc/>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public sealed override int Count => Inner.Count;

    /// <inheritdoc/>
    public sealed override bool Contains(T item) => Inner.Contains(item);

    /// <inheritdoc/>
    public bool IsProperSubsetOf(IEnumerable<T> other) => Inner.IsProperSubsetOf(other);
    /// <inheritdoc/>
    public bool IsProperSupersetOf(IEnumerable<T> other) => Inner.IsProperSupersetOf(other);

    /// <inheritdoc/>
    public bool IsSubsetOf(IEnumerable<T> other) => Inner.IsSubsetOf(other);
    /// <inheritdoc/>
    public bool IsSupersetOf(IEnumerable<T> other) => Inner.IsSupersetOf(other);

    /// <inheritdoc/>
    public bool Overlaps(IEnumerable<T> other) => Inner.Overlaps(other);

    /// <inheritdoc/>
    public bool SetEquals(IEnumerable<T> other) => Inner.SetEquals(other);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"readonly HashSet<{typeof(T).Name}>";

    /// <inheritdoc/>
    public override IEnumerator<T> GetEnumerator() => ReusableEnumerator ?? Inner.GetEnumerator();

    internal static ReadOnlySet<T> Create(HashSet<T> set)
    {
        ArgumentNullException.ThrowIfNull(set);
        return set.Count != 0 ? new(set) : Empty;
    }

    #region ISet<~> impl

    bool ISet<T>.Add(T item) => throw new NotSupportedException();

    void ISet<T>.ExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
    void ISet<T>.IntersectWith(IEnumerable<T> other) => throw new NotSupportedException();
    void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
    void ISet<T>.UnionWith(IEnumerable<T> other) => throw new NotSupportedException();

    #endregion
}
