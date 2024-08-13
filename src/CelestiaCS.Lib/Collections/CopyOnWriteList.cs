using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CelestiaCS.Lib.Collections.Internal;
using CelestiaCS.Lib.Linq;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// Provides a list that wraps another list, but rather than modifying the source on edit,
/// it creates a copy when it is first edited.
/// </summary>
/// <typeparam name="T"> The type of the items. </typeparam>
public sealed class CopyOnWriteList<T> : IList<T>, IReadOnlyList<T>, IList
{
    private readonly IReadOnlyList<T> _source;
    private List<T>? _copy;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopyOnWriteList{T}"/> class.
    /// </summary>
    /// <param name="source"> The source collection to wrap. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public CopyOnWriteList(IReadOnlyList<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private IReadOnlyList<T> Readable => _copy ?? _source;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private List<T> Writable => _copy ?? CreateWritable();

    /// <inheritdoc cref="IList{T}.this"/>
    public T this[int index]
    {
        get => Readable[index];
        set => Writable[index] = value;
    }

    /// <inheritdoc cref="ICollection{T}.Count"/>
    public int Count => Readable.Count;

    /// <summary> Whether this collection was already modified. </summary>
    public bool IsModified => _copy != null;

    /// <inheritdoc/>
    public int IndexOf(T item) => Readable.IndexOf(item);
    /// <inheritdoc/>
    public bool Contains(T item) => Readable.Contains(item);

    /// <inheritdoc/>
    public void Add(T item) => Writable.Add(item);
    /// <inheritdoc/>
    public void Insert(int index, T item) => Writable.Insert(index, item);
    /// <inheritdoc/>
    public bool Remove(T item) => Writable.Remove(item);
    /// <inheritdoc/>
    public void RemoveAt(int index) => Writable.RemoveAt(index);

    /// <inheritdoc/>
    public void Clear()
    {
        if (_copy == null)
        {
            _copy = [];
        }
        else
        {
            _copy.Clear();
        }
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => Readable.GetEnumerator();

    bool ICollection<T>.IsReadOnly => false;

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        var copy = _copy;
        if (copy != null)
        {
            copy.CopyTo(array, arrayIndex);
            return;
        }

        if (_source is ICollection<T> collOfT)
        {
            collOfT.CopyTo(array, arrayIndex);
        }
        else
        {
            CollectionOfTImplHelper.CopyTo(this, array, arrayIndex);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private List<T> CreateWritable()
    {
        Debug.Assert(_copy == null);
        return _copy = new List<T>(_source);
    }

    #region Non-generic IList

    bool IList.IsFixedSize => false;
    bool IList.IsReadOnly => false;
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => this;

    object? IList.this[int index] { get => this[index]; set => this[index] = (T)value!; }

    int IList.Add(object? value) => VarHelper.Cast<IList>(Writable).Add(value);
    bool IList.Contains(object? value) => VarHelper.IsCompatible<T>(value) && Contains((T)value!);
    int IList.IndexOf(object? value) => VarHelper.IsCompatible<T>(value) ? IndexOf((T)value!) : -1;
    void IList.Insert(int index, object? value) => VarHelper.Cast<IList>(Writable).Insert(index, value);

    void IList.Remove(object? value)
    {
        if (VarHelper.IsCompatible<T>(value)) Remove((T)value!);
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ICollection? copy = _copy;
        if (copy != null)
        {
            copy.CopyTo(array, index);
            return;
        }

        if (_source is ICollection coll)
        {
            coll.CopyTo(array, index);
        }
        else
        {
            CollectionNGImplHelper.CopyTo(this, array, index);
        }
    }

    #endregion
}
