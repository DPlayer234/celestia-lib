using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json.Serialization;
using CelestiaCS.Lib.Linq;

namespace CelestiaCS.Lib.State;

/// <summary>
/// Represents one or many values of some type and implements value equality.
/// </summary>
/// <remarks>
/// This type is a wrapper around <see cref="ImmutableArray{T}"/>.
/// </remarks>
/// <typeparam name="T"> The type of the values. </typeparam>
[JsonConverter(typeof(OneOrManyJsonConverterFactory))]
[DebuggerDisplay("{DebugValue}")]
public readonly struct OneOrMany<T> : IEquatable<OneOrMany<T>>
{
    private readonly ImmutableArray<T> _values;

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOrMany{T}"/> struct with one value.
    /// </summary>
    /// <param name="value"> The sole value to hold. </param>
    public OneOrMany(T value) => _values = [value];

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOrMany{T}"/> struct with an array of values.
    /// </summary>
    /// <param name="values"> The values to hold. Immutable arrays don't need to be copied. </param>
    public OneOrMany(ImmutableArray<T> values) => _values = values;

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOrMany{T}"/> struct with a copy of the provided collection.
    /// </summary>
    /// <param name="values"> The values to hold. The collection's content is copied. </param>
    public OneOrMany(IEnumerable<T> values) => _values = values.ToImmutableArray();

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private object? DebugValue => _values is { IsDefault: false } v ? (v.Length == 1 ? v[0] : v) : NoneDebuggerValue.instance;

    /// <summary>
    /// Gets the element at the specified index in this collection.
    /// </summary>
    /// <param name="index"> The index. </param>
    /// <returns> The item at that index. </returns>
    public T this[int index] => AsArray()[index];

    /// <summary> Gets the amount of items in this collection. </summary>
    public int Count => _values is { IsDefault: false } v ? v.Length : 0;

    /// <summary> Gets the only or first value. Returns <see langword="default"/> if empty. </summary>
    public T? Value => _values is { IsDefaultOrEmpty: false } v ? v[0] : default;

    /// <summary>
    /// Gets this collection as an immutable array.
    /// </summary>
    /// <returns> An immutable array with the same contents. </returns>
    public ImmutableArray<T> AsArray() => _values.OrEmpty();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is OneOrMany<T> many && Equals(many);

    /// <inheritdoc/>
    public bool Equals(OneOrMany<T> other)
        => ArrayValueEqualityComparer<T>.Default.Equals(AsArray(), other.AsArray());

    /// <inheritdoc/>
    public override int GetHashCode()
        => ArrayValueEqualityComparer<T>.Default.GetHashCode(AsArray());

    /// <summary>
    /// Gets an enumerator for the held items.
    /// </summary>
    /// <returns> An enumerator. </returns>
    public Enumerator GetEnumerator() => new Enumerator(this);

    public struct Enumerator
    {
        private ImmutableArray<T>.Enumerator _inner;

        public Enumerator(OneOrMany<T> self)
        {
            _inner = self.AsArray().GetEnumerator();
        }

        public T Current => _inner.Current;

        public bool MoveNext() => _inner.MoveNext();
    }

    public static bool operator ==(OneOrMany<T> left, OneOrMany<T> right) => left.Equals(right);
    public static bool operator !=(OneOrMany<T> left, OneOrMany<T> right) => !(left == right);
}
