using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.Collections.Internal;
using CelestiaCS.Lib.State;

namespace CelestiaCS.Lib.Commands;

/// <summary>
/// An immutable collection of <see cref="INodeAttribute"/>s for a node.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
public sealed class NodeAttributeCollection : IReadOnlyList<INodeAttribute>, IList<INodeAttribute>, IList
{
    /// <summary> An empty collection of node attributes. </summary>
    public static NodeAttributeCollection Empty { get; } = new();

    private readonly ImmutableArray<INodeAttribute> _values;
    private readonly ByTypeCache? _byTypeCache;

    private NodeAttributeCollection(ImmutableArray<INodeAttribute> values)
    {
        Debug.Assert(!values.IsEmpty);

        _values = values;
        _byTypeCache = new(values);
    }

    private NodeAttributeCollection()
    {
        _values = ImmutableArray<INodeAttribute>.Empty;
    }

    /// <summary> Gets the number of elements in this collection. </summary>
    public int Count => _values.Length;

    /// <inheritdoc/>
    public INodeAttribute this[int index] => _values[index];

    /// <summary>
    /// Gets the first attribute of the specified type stored in this collection.
    /// </summary>
    /// <typeparam name="T"> The attribute type. </typeparam>
    /// <returns> The first attribute of the specified type. </returns>
    public T? Get<T>() where T : class, INodeAttribute
    {
        return (T?)_byTypeCache?.Read(GenericKey<T>.Instance).FirstOrDefault();
    }

    /// <summary>
    /// Gets all attributes of the specified type stored in this collection.
    /// </summary>
    /// <typeparam name="T"> The attribute type. </typeparam>
    /// <returns> All attributes of the specified type. </returns>
    public ImmutableArray<T> GetAll<T>() where T : class, INodeAttribute
    {
        return _byTypeCache?.Read(GenericKey<T>.Instance).CastArray<T>() ?? ImmutableArray<T>.Empty;
    }

    /// <summary>
    /// Gets the first attribute of the specified type stored in this collection without accessing the cache.
    /// </summary>
    /// <typeparam name="T"> The attribute type. </typeparam>
    /// <returns> The first attribute of the specified type. </returns>
    public T? GetNoCache<T>() where T : class, INodeAttribute
    {
        foreach (var item in _values)
        {
            if (item is T)
                return Unsafe.As<T>(item);
        }

        return null;
    }

    /// <summary>
    /// Determines whether this collection contains the specified attribute.
    /// </summary>
    /// <param name="item"> The attribute to look for. </param>
    /// <returns> Whether the attribute is contained. </returns>
    public bool Contains(INodeAttribute item) => _values.Contains(item);

    /// <summary>
    /// Determines the index of the specified attribute within this collection.
    /// </summary>
    /// <param name="item"> The attribute to look for. </param>
    /// <returns> The index of the attribute, or -1 if not found. </returns>
    public int IndexOf(INodeAttribute item) => _values.IndexOf(item);

    /// <summary>
    /// Creates a <see cref="NodeAttributeCollection"/> with the specified elements.
    /// </summary>
    /// <param name="source"> The attributes for the collection. </param>
    /// <returns> A matching immutable collection. </returns>
    public static NodeAttributeCollection Create(IEnumerable<INodeAttribute> source)
    {
        return Create(source.ToImmutableArray());
    }

    /// <inheritdoc cref="Create(IEnumerable{INodeAttribute})">
    public static NodeAttributeCollection Create(ImmutableArray<INodeAttribute> source)
    {
        if (source.IsEmpty) return Empty;
        return new NodeAttributeCollection(source);
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new Enumerator(this);

    #region Explicit interface implementations

    #region Generic

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool ICollection<INodeAttribute>.IsReadOnly => true;

    INodeAttribute IList<INodeAttribute>.this[int index] { get => this[index]; set => throw new NotSupportedException(); }

    IEnumerator<INodeAttribute> IEnumerable<INodeAttribute>.GetEnumerator() => VarHelper.Cast<IEnumerable<INodeAttribute>>(_values).GetEnumerator();

    void ICollection<INodeAttribute>.CopyTo(INodeAttribute[] array, int arrayIndex)
        => CollectionOfTImplHelper.CopyTo(ImmutableCollectionsMarshal.AsArray(_values)!, _values.Length, array, arrayIndex);

    void ICollection<INodeAttribute>.Add(INodeAttribute item) => throw new NotSupportedException();
    bool ICollection<INodeAttribute>.Remove(INodeAttribute item) => throw new NotSupportedException();
    void ICollection<INodeAttribute>.Clear() => throw new NotSupportedException();

    void IList<INodeAttribute>.Insert(int index, INodeAttribute item) => throw new NotSupportedException();
    void IList<INodeAttribute>.RemoveAt(int index) => throw new NotSupportedException();

    #endregion

    #region Non-generic

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool ICollection.IsSynchronized => true;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    object ICollection.SyncRoot => this;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool IList.IsFixedSize => true;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool IList.IsReadOnly => true;

    object? IList.this[int index] { get => this[index]; set => throw new NotSupportedException(); }

    IEnumerator IEnumerable.GetEnumerator() => VarHelper.Cast<IEnumerable>(_values).GetEnumerator();

    void ICollection.CopyTo(Array array, int index)
        => CollectionNGImplHelper.CopyTo(ImmutableCollectionsMarshal.AsArray(_values)!, _values.Length, array, index);

    bool IList.Contains(object? value) => value is null or INodeAttribute && Contains((INodeAttribute)value!);
    int IList.IndexOf(object? value) => value is null or INodeAttribute ? IndexOf((INodeAttribute)value!) : -1;

    int IList.Add(object? value) => throw new NotSupportedException();
    void IList.Clear() => throw new NotSupportedException();

    void IList.Insert(int index, object? value) => throw new NotSupportedException();
    void IList.Remove(object? value) => throw new NotSupportedException();
    void IList.RemoveAt(int index) => throw new NotSupportedException();

    #endregion

    #endregion

    /// <summary>
    /// An enumerator over the elements in a <see cref="NodeAttributeCollection"/>.
    /// </summary>
    public struct Enumerator
    {
        private ImmutableArray<INodeAttribute>.Enumerator _enumerator;

        internal Enumerator(NodeAttributeCollection self) => _enumerator = self._values.GetEnumerator();

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public INodeAttribute Current => _enumerator.Current;
        /// <inheritdoc cref="IEnumerator.MoveNext"/>
        public bool MoveNext() => _enumerator.MoveNext();
    }

    private sealed class ByTypeCache : RareWriteFactoryCache<GenericKey, ImmutableArray<INodeAttribute>>
    {
        private readonly ImmutableArray<INodeAttribute> _values;

        public ByTypeCache(ImmutableArray<INodeAttribute> values) : base(ReferenceEqualityComparer.Instance)
        {
            _values = values;
        }

        protected override ImmutableArray<INodeAttribute> Create(GenericKey key)
        {
            return key.Filter(_values);
        }
    }

    private abstract class GenericKey
    {
        public abstract ImmutableArray<INodeAttribute> Filter(ImmutableArray<INodeAttribute> source);
    }

    private sealed class GenericKey<TOf> : GenericKey
        where TOf : class, INodeAttribute
    {
        public static readonly GenericKey<TOf> Instance = new();
        public override ImmutableArray<INodeAttribute> Filter(ImmutableArray<INodeAttribute> source) => ImmutableArray<INodeAttribute>.CastUp(source.OfType<TOf>().ToImmutableArray());
    }
}
