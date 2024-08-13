using System;
using System.Collections;
using System.Collections.Generic;
using CelestiaCS.Lib.Collections.Internal;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// Provides static methods to create <seealso cref="SingleList{T}"/> instances, read-only lists with 1 element.
/// </summary>
public static class SingleList
{
    /// <summary>
    /// Creates a <seealso cref="SingleList{T}"/> with <paramref name="value"/> as the element.
    /// </summary>
    /// <typeparam name="T"> The type of the element. </typeparam>
    /// <param name="value"> The value to hold. </param>
    /// <returns> A collection that holds only <paramref name="value"/>. </returns>
    public static SingleList<T> Of<T>(T value) => new(value);
}

/// <summary>
/// A collection/list that always contains exactly 1 element. This class is read-only.
/// </summary>
/// <typeparam name="T"> The type of the element. </typeparam>
public class SingleList<T> : IReadOnlyList<T>, IList<T>, IAsSpan<T>, IList
{
    private readonly T _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleList{T}"/> class.
    /// </summary>
    /// <param name="value"> The value to hold. </param>
    public SingleList(T value)
    {
        _value = value;
    }

    /// <summary> Gets the held value. </summary>
    public T Value => _value;

    /// <inheritdoc/>
    public T this[int index]
    {
        get
        {
            if (index != 0)
                ThrowHelper.ArgumentOutOfRange_IndexMustBeLess();

            return _value;
        }
    }

    /// <inheritdoc/>
    public int Count => 1;

    /// <inheritdoc/>
    public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_value, item);

    /// <inheritdoc/>
    public int IndexOf(T item) => Contains(item) ? 0 : -1;

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        yield return _value;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    ReadOnlySpan<T> IAsSpan<T>.AsSpan() => new ReadOnlySpan<T>(in _value);

    #region ICollection<T>

    bool ICollection<T>.IsReadOnly => true;

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        CollectionOfTImplHelper.ValidateCopyToArguments(1, array, arrayIndex);
        array[arrayIndex] = _value;
    }

    void ICollection<T>.Add(T item) => throw new NotSupportedException();
    void ICollection<T>.Clear() => throw new NotSupportedException();
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

    #endregion

    #region IList<T>

    T IList<T>.this[int index] { get => this[index]; set => throw new NotSupportedException(); }

    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

    #endregion

    #region ICollection

    bool ICollection.IsSynchronized => true;
    object ICollection.SyncRoot => this;

    void ICollection.CopyTo(Array array, int index)
    {
        CollectionNGImplHelper.ValidateCopyToArguments(1, array, index);

        try
        {
            array.SetValue(_value, index);
        }
        catch (InvalidCastException)
        {
            CollectionNGImplHelper.ThrowInvalidArrayType();
        }
    }

    #endregion

    #region IList

    bool IList.IsFixedSize => true;
    bool IList.IsReadOnly => true;

    object? IList.this[int index] { get => this[index]; set => throw new NotSupportedException(); }

    bool IList.Contains(object? value) => value is T t && Contains(t);
    int IList.IndexOf(object? value) => value is T t && Contains(t) ? 0 : -1;

    int IList.Add(object? value) => throw new NotSupportedException();
    void IList.Clear() => throw new NotSupportedException();
    void IList.Insert(int index, object? value) => throw new NotSupportedException();
    void IList.Remove(object? value) => throw new NotSupportedException();
    void IList.RemoveAt(int index) => throw new NotSupportedException();

    #endregion
}
