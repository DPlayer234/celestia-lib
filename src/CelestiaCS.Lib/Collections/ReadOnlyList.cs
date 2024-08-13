using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.Collections.Internal;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// Provides static helper method for <see cref="ReadOnlyList{T}"/>.
/// </summary>
public static class ReadOnlyList
{
    /// <summary>
    /// Creates a new <see cref="ReadOnlyList{T}"/> by wrapping the <paramref name="array"/>.
    /// If the <paramref name="array"/> is empty, returns <see cref="ReadOnlyList{T}.Empty"/> instead.
    /// The array is never copied.
    /// </summary>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="array"> The array to wrap. </param>
    /// <returns> A read-only list wrapping the array or equivalent. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="array"/> is null. </exception>
    public static ReadOnlyList<T> Create<T>(T[] array)
        => ReadOnlyList<T>.Create(array);

    /// <summary>
    /// Creates a new <see cref="ReadOnlyList{T}"/> by wrapping the <paramref name="array"/>.
    /// If the <paramref name="array"/> is empty, returns <see cref="ReadOnlyList{T}.Empty"/> instead.
    /// The array is never copied.
    /// </summary>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="array"> The array to wrap. </param>
    /// <returns> A read-only list wrapping the array or equivalent. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="array"/> is default. </exception>
    public static ReadOnlyList<T> Create<T>(ImmutableArray<T> array)
    {
        // It is fine to construct a ReadOnlyList from an immutable array like this
        // since the backing array is never exposed and internal methods won't mutate it.
        // ... Also, yes, this throws an ArgumentNullException when passed default.
        return Create(ImmutableCollectionsMarshal.AsArray(array)!);
    }

    /// <summary>
    /// Provided for compiler use with collection expressions.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ReadOnlyList<T> CreateFromSpan<T>(ReadOnlySpan<T> span)
        => Create(span.ToArray());
}

/// <summary>
/// A read-only list created by wrapping an array.
/// </summary>
/// <typeparam name="T"> The type of the items. </typeparam>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[CollectionBuilder(typeof(ReadOnlyList), nameof(ReadOnlyList.CreateFromSpan))]
public sealed class ReadOnlyList<T>
    : ReadOnlyCollectionBase<T, T[]>
    , IReadOnlyList<T>
    , IList<T>
    , IList
    , IAsSpan<T>
{
    /// <summary> The shared, empty list instance. </summary>
    public static ReadOnlyList<T> Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyList{T}"/> class by wrapping an array instance.
    /// The array is not copied.
    /// </summary>
    /// <param name="array"> The array to wrap. </param>
    private ReadOnlyList(T[] array) : base(array) { }

    /// <summary>
    /// Initializes an empty instance of the <see cref="ReadOnlyList{T}"/> class.
    /// </summary>
    private ReadOnlyList() : base([]) { ReusableEnumerator = NullImpl.EnumeratorOf<T>(); }

    /// <inheritdoc/>
    public T this[int index] => Inner[index];

    /// <inheritdoc/>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public override int Count => Inner.Length;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"readonly {typeof(T).Name}[]";

    /// <inheritdoc/>
    public override bool Contains(T item) => IndexOf(item) != -1;
    /// <inheritdoc/>
    public int IndexOf(T item) => Array.IndexOf(Inner, item);

    // Iterating this type directly is common enough, so let's provide a struct-based enumerator.
    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public new Enumerator GetEnumerator() => new Enumerator(Inner);

    private IEnumerator<T> GetClsEnumerator() => ReusableEnumerator ?? StructEnumerator.WrapArray(Inner);

    internal static ReadOnlyList<T> Create(T[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        return array.Length != 0 ? new(array) : Empty;
    }

    // Reimplement GetEnumerator for the interface since we cannot both "new" it and override it.
    // This should stay functionally identical to what the base impl would do, but without another virtual call.
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetClsEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetClsEnumerator();

    /// <inheritdoc/>
    public ReadOnlySpan<T> AsSpan() => Inner;

    #region IList<~> impl

    T IList<T>.this[int index] { get => this[index]; set => throw new NotSupportedException(); }

    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

    #endregion

    #region IList impl

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool IList.IsFixedSize => true;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool IList.IsReadOnly => true;

    object? IList.this[int index] { get => this[index]; set => throw new NotSupportedException(); }

    bool IList.Contains(object? value) => Array.IndexOf(Inner, value) != -1;
    int IList.IndexOf(object? value) => Array.IndexOf(Inner, value);

    int IList.Add(object? value) => throw new NotSupportedException();
    void IList.Clear() => throw new NotSupportedException();
    void IList.Insert(int index, object? value) => throw new NotSupportedException();
    void IList.Remove(object? value) => throw new NotSupportedException();
    void IList.RemoveAt(int index) => throw new NotSupportedException();

    #endregion

    #region Enumerator

    public struct Enumerator
    {
        private ArrayEnumerator<T> _enumerator;

        public Enumerator(T[] array) => _enumerator = new(array);

        public readonly T Current => _enumerator.Current;
        public bool MoveNext() => _enumerator.MoveNext();
    }

    #endregion
}
