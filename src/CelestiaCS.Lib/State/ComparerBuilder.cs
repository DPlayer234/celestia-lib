using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CelestiaCS.Lib.Collections;

namespace CelestiaCS.Lib;

/// <summary>
/// Allows building a comparer by chaining multiple properties to compare in order.
/// </summary>
/// <typeparam name="T"> The type to compare. </typeparam>
public sealed class ComparerBuilder<T> : Comparer<T>
{
    private ValueList<Comparison<T>> _comparers;

    /// <summary> Starts a new builder, comparing by this field first. </summary>
    /// <typeparam name="TBy"> The field's type. </typeparam>
    /// <param name="field"> A delegate returning the field. </param>
    /// <returns> A new comparer builder. </returns>
    public static ComparerBuilder<T> By<TBy>(Func<T, TBy> field)
    {
        return new() { _comparers = { field.ToCompare } };
    }

    /// <summary> Adds a comparison for another field. </summary>
    /// <typeparam name="TBy"> The field's type. </typeparam>
    /// <param name="field"> A delegate returning the field. </param>
    /// <returns> The same builder. </returns>
    public ComparerBuilder<T> ThenBy<TBy>(Func<T, TBy> field)
    {
        _comparers.Add(field.ToCompare);
        return this;
    }

    /// <inheritdoc/>
    public override int Compare(T? x, T? y) => CompareCore(x, y, _comparers.AsSpan());

    /// <summary> Creates an immutable comparer, equal to the current state of this instance. </summary>
    /// <returns> An equivalent, immutable comparer. </returns>
    public Comparer<T> ToImmutable() => new Immutable(_comparers.ToImmutableArray());

    private sealed class Immutable(ImmutableArray<Comparison<T>> comparers) : Comparer<T>
    {
        public override int Compare(T? x, T? y) => CompareCore(x, y, comparers.AsSpan());
    }

    private static int CompareCore(T? x, T? y, ReadOnlySpan<Comparison<T>> comparers)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        foreach (var del in comparers)
        {
            int r = del(x, y);
            if (r != 0) return r;
        }

        return 0;
    }
}

file static class Ext
{
    public static int ToCompare<T, TBy>(this Func<T, TBy> field, T a, T b)
    {
        return Comparer<TBy>.Default.Compare(field(a), field(b));
    }
}
