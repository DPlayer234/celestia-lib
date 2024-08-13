using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// Defines a small wrapper around a <see cref="ConcurrentDictionary{TKey, TValue}"/> to be used as a set.
/// The <see langword="default"/> value is invalid.
/// </summary>
/// <typeparam name="T"> The type of the items. </typeparam>
public readonly struct ConcurrentSet<T>
    where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _dict;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentSet{T}"/> struct.
    /// </summary>
    public ConcurrentSet()
    {
        _dict = new();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentSet{T}"/> struct.
    /// </summary>
    /// <param name="comparer"> The comparer to use for the value. </param>
    public ConcurrentSet(IEqualityComparer<T>? comparer)
    {
        _dict = new(comparer);
    }

    /// <summary>
    /// Tries to add an item to the set.
    /// </summary>
    /// <param name="item"> The item to add. </param>
    /// <returns> <see langword="true"/> if the item was added; <see langword="false"/> if it was already present. </returns>
    public bool TryAdd(T item) => _dict.TryAdd(item, 0);

    /// <summary>
    /// Tries to remove an item from the set.
    /// </summary>
    /// <param name="item"> The item to remove. </param>
    /// <returns> <see langword="true"/> if the item was present and removed; <see langword="false"/> if it was not present. </returns>
    public bool TryRemove(T item) => _dict.TryRemove(item, out _);

    /// <summary>
    /// Determines whether an item is part of the set.
    /// </summary>
    /// <param name="item"> The item to check. </param>
    /// <returns> Whether the item is part of the set. </returns>
    public bool Contains(T item) => _dict.ContainsKey(item);

    /// <summary>
    /// Removes all items from the set.
    /// </summary>
    public void Clear() => _dict.Clear();

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new Enumerator(_dict);

    public readonly struct Enumerator : IDisposable
    {
        private readonly IEnumerator<KeyValuePair<T, byte>> _src;

        internal Enumerator(ConcurrentDictionary<T, byte> src)
        {
            _src = src.GetEnumerator();
        }

        public T Current => _src.Current.Key;
        public bool MoveNext() => _src.MoveNext();
        public void Dispose() => _src.Dispose();
    }
}
