using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// Provides static helper method for <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
/// </summary>
public static class ReadOnlyDictionary
{
    /// <summary>
    /// Creates a new <see cref="ReadOnlyDictionary{TKey, TValue}"/> by wrapping the <paramref name="dictionary"/>.
    /// If the <paramref name="dictionary"/> is empty, returns <see cref="ReadOnlyDictionary{TKey, TValue}.Empty"/> instead.
    /// The dictionary is never copied.
    /// </summary>
    /// <param name="dictionary"> The dictionary to wrap. </param>
    /// <returns> A read-only dictionary wrapping the dictionary or equivalent. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="dictionary"/> is null. </exception>
    public static ReadOnlyDictionary<TKey, TValue> Create<TKey, TValue>(Dictionary<TKey, TValue> dictionary) where TKey : notnull
        => ReadOnlyDictionary<TKey, TValue>.Create(dictionary);
}

/// <summary>
/// A read-only dictionary created by wrapping a <see cref="Dictionary{TKey, TValue}"/>.
/// </summary>
/// <typeparam name="TKey"> The type of the keys. </typeparam>
/// <typeparam name="TValue"> The type of the values. </typeparam>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class ReadOnlyDictionary<TKey, TValue>
    : ReadOnlyCollectionBase<KeyValuePair<TKey, TValue>, Dictionary<TKey, TValue>>
    , IReadOnlyDictionary<TKey, TValue>
    , IDictionary<TKey, TValue>
    , IDictionary
    where TKey : notnull
{
    /// <summary> The shared, empty dictionary instance. </summary>
    public static ReadOnlyDictionary<TKey, TValue> Empty { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyDictionary{TKey, TValue}"/> class by wrapping a dictionary instance.
    /// The dictionary is not copied.
    /// </summary>
    /// <param name="dictionary"> The dictionary to wrap. </param>
    private ReadOnlyDictionary(Dictionary<TKey, TValue> dictionary) : base(dictionary) { }

    /// <summary>
    /// Initializes an empty instance of the <see cref="ReadOnlyDictionary{TKey, TValue}"/> class.
    /// </summary>
    private ReadOnlyDictionary() : base([]) { ReusableEnumerator = NullImpl.EnumeratorOf<KeyValuePair<TKey, TValue>>(); }

    /// <inheritdoc/>
    public TValue this[TKey key] => Inner[key];

    /// <inheritdoc/>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public override int Count => Inner.Count;

    /// <inheritdoc/>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IEnumerable<TKey> Keys => Inner.Keys;
    /// <inheritdoc/>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IEnumerable<TValue> Values => Inner.Values;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"readonly Dictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>";

    /// <inheritdoc/>
    public bool ContainsKey(TKey key) => Inner.ContainsKey(key);
    /// <inheritdoc/>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Inner.TryGetValue(key, out value);

    /// <inheritdoc/>
    public override bool Contains(KeyValuePair<TKey, TValue> item) => Inner.TryGetValue(item.Key, out var v) && EqualityComparer<TValue>.Default.Equals(item.Value, v);

    /// <inheritdoc/>
    public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => ReusableEnumerator ?? Inner.GetEnumerator();

    internal static ReadOnlyDictionary<TKey, TValue> Create(Dictionary<TKey, TValue> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return dictionary.Count != 0 ? new(dictionary) : Empty;
    }

    #region IDictionary<~> impl

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ICollection<TKey> IDictionary<TKey, TValue>.Keys => Inner.Keys;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ICollection<TValue> IDictionary<TKey, TValue>.Values => Inner.Values;

    TValue IDictionary<TKey, TValue>.this[TKey key] { get => this[key]; set => throw new NotSupportedException(); }

    void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => throw new NotSupportedException();
    bool IDictionary<TKey, TValue>.Remove(TKey key) => throw new NotSupportedException();

    #endregion

    #region IDictionary impl

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool IDictionary.IsFixedSize => true;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool IDictionary.IsReadOnly => true;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ICollection IDictionary.Keys => Inner.Keys;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ICollection IDictionary.Values => Inner.Values;

    object? IDictionary.this[object key] { get => CheckKey(key) && TryGetValue((TKey)key, out TValue? value) ? value : null; set => throw new NotSupportedException(); }

    bool IDictionary.Contains(object key) => CheckKey(key) && ContainsKey((TKey)key);
    IDictionaryEnumerator IDictionary.GetEnumerator() => Inner.GetEnumerator();

    void IDictionary.Add(object key, object? value) => throw new NotSupportedException();
    void IDictionary.Clear() => throw new NotSupportedException();
    void IDictionary.Remove(object key) => throw new NotSupportedException();

    private bool CheckKey(object key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key is TKey;
    }

    #endregion
}
