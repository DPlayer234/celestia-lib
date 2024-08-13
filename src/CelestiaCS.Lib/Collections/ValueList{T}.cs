using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CelestiaCS.Lib.Collections.ArrayAllocators;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.Collections;

/* When adding a method, consider adding it to ValueList{T, TAlloc} as well!! */

/// <summary>
/// A value-type list to hold items. This struct should not be used after being copied.
/// </summary>
/// <remarks>
/// You should avoid boxing this struct. Use it as a small temporary list, f.e. to build a span or array, or as a private list.
/// </remarks>
/// <typeparam name="T"> The type of items to hold. </typeparam>
[DebuggerDisplay("Count = {Count}")]
public struct ValueList<T> : IList<T>, IReadOnlyList<T>, IAsSpan<T>, IList, IValueList
{
    // ValueList<T> is implemented over a ValueList<T, TAlloc> that uses an allocator that simply
    // creates new arrays as usual. As such, it does not expose a Dispose() method.

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private ValueList<T, NewArrayAllocator> _core;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueList{T}"/> struct with the specified capacity.
    /// </summary>
    /// <param name="capacity"> The capacity to initialize it to. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="capacity"/> is negative. </exception>
    public ValueList(int capacity)
    {
        if (capacity <= 0)
        {
            if (capacity < 0)
                ThrowHelper.ArgumentOutOfRange_CapacityMustBePositive();

            this = default;
            return;
        }

        _core = new(new T[capacity], 0);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueList{T}"/> struct by wrapping a given array.
    /// The array will be discarded when resizing.
    /// </summary>
    /// <param name="array"> The array to wrap. </param>
    /// <exception cref="ArrayTypeMismatchException"> <typeparamref name="T"/> is a reference type and the type of <paramref name="array"/> doesn't exactly match. </exception>
    public ValueList(T[]? array)
    {
        if (array == null)
        {
            this = default;
            return;
        }

        if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
            ThrowHelper.ArrayTypeMismatch();

        _core = new(array, array.Length);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueList{T}"/> by wrapping a given array as a buffer.
    /// The array will be discarded when resizing.
    /// The count specifies how much of the array to consider as the list, the remainder is used as open buffer.
    /// </summary>
    /// <param name="array"> The array to wrap. </param>
    /// <param name="count"> The count of assigned items. </param>
    /// <exception cref="ArrayTypeMismatchException"> <typeparamref name="T"/> is a reference type and the type of <paramref name="array"/> doesn't exactly match. </exception>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="count"/> is negative or greater than the <paramref name="array"/> length. </exception>
    public ValueList(T[]? array, int count)
    {
        if (array == null)
        {
            if (count != 0)
                ThrowHelper.ArgumentOutOfRange();

            this = default;
            return;
        }

        if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
            ThrowHelper.ArrayTypeMismatch();

        if ((uint)count > (uint)array.Length)
            ThrowHelper.ArgumentOutOfRange();

        _core = new(array, count);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.this[int]"/>
    public readonly ref T this[int index] => ref _core[index];

    /// <inheritdoc cref="ValueList{T, TAlloc}.Count"/>
    public readonly int Count => _core.Count;

    /// <inheritdoc cref="ValueList{T, TAlloc}.Capacity"/>
    public readonly int Capacity => _core.Capacity;

    /// <inheritdoc cref="ValueList{T, TAlloc}.IsAllocated"/>
    public readonly bool IsAllocated => _core.IsAllocated;

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Debugger helper.")]
    private readonly Span<T> Content => AsSpan();

    /// <inheritdoc cref="ValueList{T, TAlloc}.Add(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        _core.Add(item);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.AddRange(ReadOnlySpan{T})"/>
    public void AddRange(ReadOnlySpan<T> items)
    {
        _core.AddRange(items);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.AddRange{TDerived}(ReadOnlySpan{TDerived})"/>
    public void AddRange<TDerived>(ReadOnlySpan<TDerived> items) where TDerived : class?, T
    {
        _core.AddRange(items);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.AddRange(IEnumerable{T})"/>
    public void AddRange(IEnumerable<T> items)
    {
        _core.AddRange(items);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.Remove(T)"/>
    public bool Remove(T item)
    {
        return _core.Remove(item);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.RemoveAt(int)"/>
    public void RemoveAt(int index)
    {
        _core.RemoveAt(index);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.RemoveAt(Index)"/>
    public void RemoveAt(Index index)
    {
        _core.RemoveAt(index);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.RemoveRange(int, int)"/>
    public void RemoveRange(int index, int count)
    {
        _core.RemoveRange(index, count);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.RemoveRange(Range)"/>
    public void RemoveRange(Range range)
    {
        _core.RemoveRange(range);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.Insert(int, T)"/>
    public void Insert(int index, T item)
    {
        _core.Insert(index, item);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.Insert(Index, T)"/>
    public void Insert(Index index, T item)
    {
        _core.Insert(index, item);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.InsertRange(int, ReadOnlySpan{T})"/>
    public void InsertRange(int index, ReadOnlySpan<T> items)
    {
        _core.InsertRange(index, items);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.InsertRange(Index, ReadOnlySpan{T})"/>
    public void InsertRange(Index index, ReadOnlySpan<T> items)
    {
        _core.InsertRange(index, items);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.InsertRange{TDerived}(int, ReadOnlySpan{TDerived})"/>
    public void InsertRange<TDerived>(int index, ReadOnlySpan<TDerived> items) where TDerived : class?, T
    {
        _core.InsertRange(index, items);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.InsertRange{TDerived}(Index, ReadOnlySpan{TDerived})"/>
    public void InsertRange<TDerived>(Index index, ReadOnlySpan<TDerived> items) where TDerived : class?, T
    {
        _core.InsertRange(index, items);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.InsertRange(int, IEnumerable{T})"/>
    public void InsertRange(int index, IEnumerable<T> items)
    {
        _core.InsertRange(index, items);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.InsertRange(Index, IEnumerable{T})"/>
    public void InsertRange(Index index, IEnumerable<T> items)
    {
        _core.InsertRange(index, items);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.IndexOf(T)"/>
    public readonly int IndexOf(T item)
    {
        return _core.IndexOf(item);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.LastIndexOf(T)"/>
    public readonly int LastIndexOf(T item)
    {
        return _core.LastIndexOf(item);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.Contains(T)"/>
    public readonly bool Contains(T item)
    {
        return _core.Contains(item);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.Reserve(int)"/>
    public void Reserve(int count)
    {
        _core.Reserve(count);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.EnsureCapacity(int)"/>
    public void EnsureCapacity(int capacity)
    {
        _core.EnsureCapacity(capacity);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.Any()"/>
    public readonly bool Any()
    {
        return _core.Any();
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.Any(Func{T, bool})"/>
    public readonly bool Any(Func<T, bool> predicate)
    {
        return _core.Any(predicate);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.All(Func{T, bool})"/>
    public readonly bool All(Func<T, bool> predicate)
    {
        return _core.All(predicate);
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.Clear"/>
    public void Clear()
    {
        _core.Clear();
    }

    /// <summary>
    /// Trims the internal storage to the required size.
    /// </summary>
    public void TrimExcess()
    {
        if (_core._array == null)
            return;

        int count = Count;
        if (count != 0)
            Array.Resize(ref _core._array, count);
        else
            _core._array = null;
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.IsLinkedTo(ValueList{T, TAlloc})"/>
    public readonly bool IsLinkedTo(ValueList<T> other)
    {
        return _core.IsLinkedTo(other._core);
    }

    #region Casts

    /// <inheritdoc cref="ValueList{T, TAlloc}.AsSpan"/>
    public readonly Span<T> AsSpan()
    {
        return _core.AsSpan();
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.AsReadOnlySpan"/>
    public readonly ReadOnlySpan<T> AsReadOnlySpan()
    {
        return _core.AsReadOnlySpan();
    }

    /// <summary>
    /// Creates an array with the same contents as this list, draining this instance and resetting it to <see langword="default"/>.
    /// </summary>
    /// <remarks>
    /// This attempts to reuse the internal array if the count and capacity match.
    /// </remarks>
    /// <returns> An array containing the data in the list. </returns>
    public T[] DrainToArray()
    {
        var self = _core;
        if (self._array == null)
            return [];

        this = default;
        if (self.Count == self._array.Length)
            return self._array;

        return self.ToArray();
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.ToArray"/>
    public readonly T[] ToArray()
    {
        return _core.ToArray();
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.ToImmutableArray"/>
    public readonly ImmutableArray<T> ToImmutableArray()
    {
        return _core.ToImmutableArray();
    }

    /// <summary>
    /// Returns a ref to this instance as the more generic <see cref="ValueList{T, TAlloc}"/> with the correctly typed allocator.
    /// </summary>
    /// <remarks>
    /// This allows writing methods only in terms of <see cref="ValueList{T, TAlloc}"/> without duplicating code for <see cref="ValueList{T}"/>.
    /// </remarks>
    /// <returns> A reference to this instance. </returns>
    [UnscopedRef]
    public ref ValueList<T, NewArrayAllocator> WithAllocator()
    {
        return ref _core;
    }

    #endregion

    /// <inheritdoc cref="ValueList{T, TAlloc}.Clone"/>
    public readonly ValueList<T> Clone()
    {
        ValueList<T> result;
        result._core = _core.Clone();
        return result;
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.Clone{TNewAlloc}"/>
    public readonly ValueList<T, TNewAlloc> Clone<TNewAlloc>()
        where TNewAlloc : IArrayAllocator
    {
        return _core.Clone<TNewAlloc>();
    }

    /// <inheritdoc cref="ValueList{T, TAlloc}.ToEnumerable"/>
    public readonly IEnumerable<T> ToEnumerable()
    {
        return _core.ToEnumerable();
    }

    /// <summary>
    /// Returns an enumerator that can be used to iterate over this collection.
    /// </summary>
    /// <remarks>
    /// While the enumerator is in use, avoid removing elements from the collection.
    /// </remarks>
    /// <returns> An enumerator for this list. </returns>
    public readonly ValueListEnumerator<T> GetEnumerator() => _core.GetEnumerator();

    #region Generic collection interfaces

    T IList<T>.this[int index] { readonly get => _core[index]; set => _core[index] = value; }
    readonly T IReadOnlyList<T>.this[int index] => _core[index];

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly bool ICollection<T>.IsReadOnly => false;

    readonly void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _core.CopyTo(array, arrayIndex);

    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => _core.GetClsEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => _core.GetClsEnumerator();

    readonly ReadOnlySpan<T> IAsSpan<T>.AsSpan() => _core.AsReadOnlySpan();

    #endregion

    #region Non-generic collection interfaces

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly Array? IValueList.Array => _core._array;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly bool IList.IsFixedSize => false;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly bool IList.IsReadOnly => false;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly bool ICollection.IsSynchronized => false;

    object? IList.this[int index] { readonly get => _core[index]; set => _core[index] = (T)value!; }

    int IList.Add(object? value) => _core.AddObject(value);
    void IList.Insert(int index, object? value) => _core.Insert(index, (T)value!);
    readonly bool IList.Contains(object? value) => VarHelper.IsCompatible<T>(value) && _core.Contains((T)value!);
    readonly int IList.IndexOf(object? value) => VarHelper.IsCompatible<T>(value) ? _core.IndexOf((T)value!) : -1;
    void IList.Remove(object? value) => _core.RemoveObject(value);

    readonly void ICollection.CopyTo(Array array, int index) => _core.CopyToObject(array, index);

    #endregion
}
