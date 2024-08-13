using System;
using System.Collections.Generic;
using CelestiaCS.Lib.State;

namespace CelestiaCS.Lib.Linq;

partial class EnumerableExtensions
{
    /// <summary>
    /// Returns the minimum value in collection according to a key selected by a delegate.
    /// </summary>
    /// <typeparam name="TSource"> The type of the collection elements </typeparam>
    /// <typeparam name="TKey"> The type of the comparison key. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="keySelector"> A function to select to key from each item. </param>
    /// <param name="defaultValue"> The default value to return if the sequence is empty. </param>
    /// <returns> The value with the minimum key in the sequence, or <paramref name="defaultValue"/> if it is empty. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="keySelector"/> is null. </exception>
    public static TSource? MinByOrDefault<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, TSource? defaultValue = default)
    {
        return MinByOrDefault(source, keySelector, new Identity<TSource?>(defaultValue)).Value;
    }

    /// <inheritdoc cref="MaxByOrDefault{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey}, TSource)"/>
    /// <typeparam name="TDefault"> The type of the default value and return. </typeparam>
    public static TDefault? MinByOrDefault<TSource, TKey, TDefault>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, TDefault? defaultValue)
        where TDefault : ICastFrom<TDefault, TSource?>?
    {
        return MinMaxByOrDefaultCore(source, keySelector, defaultValue, new MinOrder<TKey>());
    }

    /// <summary>
    /// Returns the maximum value in collection according to a key selected by a delegate.
    /// </summary>
    /// <typeparam name="TSource"> The type of the collection elements </typeparam>
    /// <typeparam name="TKey"> The type of the comparison key. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="keySelector"> A function to select to key from each item. </param>
    /// <param name="defaultValue"> The default value to return if the sequence is empty. </param>
    /// <returns> The value with the maximum key in the sequence, or <paramref name="defaultValue"/> if it is empty. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="keySelector"/> is null. </exception>
    public static TSource? MaxByOrDefault<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, TSource? defaultValue = default)
    {
        return MaxByOrDefault(source, keySelector, new Identity<TSource?>(defaultValue)).Value;
    }

    /// <inheritdoc cref="MaxByOrDefault{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey}, TSource)"/>
    /// <typeparam name="TDefault"> The type of the default value and return. </typeparam>
    public static TDefault? MaxByOrDefault<TSource, TKey, TDefault>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, TDefault? defaultValue)
        where TDefault : ICastFrom<TDefault, TSource?>?
    {
        return MinMaxByOrDefaultCore(source, keySelector, defaultValue, new MaxOrder<TKey>());
    }

    private static TDefault? MinMaxByOrDefaultCore<TSource, TKey, TDefault, TOrder>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, TDefault? defaultValue, TOrder order)
        where TDefault : ICastFrom<TDefault, TSource?>?
        where TOrder : IOrder<TKey>
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        using var e = source.GetEnumerator();
        if (!e.MoveNext()) return defaultValue;

        TSource value = e.Current;
        TKey key = keySelector(value);

        while (e.MoveNext())
        {
            TSource nextValue = e.Current;
            TKey nextKey = keySelector(nextValue);
            if (order.IsBetter(nextKey, key))
            {
                key = nextKey;
                value = nextValue;
            }
        }

        return TDefault.From(value);
    }

    private readonly struct Identity<T>(T value) : ICastFrom<Identity<T>, T>
    {
        public T Value => value;
        public static Identity<T> From(T other) => new(other);
    }

    private interface IOrder<T>
    {
        bool IsBetter(T? value, T? previous);
    }

    // The default comparer already handles null so I see no
    // reason to check for it like the BCL's Max/Min/MaxBy/MinBy do.
    private readonly struct MinOrder<T> : IOrder<T>
    {
        private readonly Comparer<T>? _comparer;
        public MinOrder() => _comparer = Comparer<T>.Default;

        public bool IsBetter(T? value, T? previous) => typeof(T).IsValueType
            ? Comparer<T>.Default.Compare(value, previous) < 0
            : _comparer!.Compare(value, previous) < 0;
    }

    private readonly struct MaxOrder<T> : IOrder<T>
    {
        private readonly Comparer<T>? _comparer;
        public MaxOrder() => _comparer = Comparer<T>.Default;

        public bool IsBetter(T? value, T? previous) => typeof(T).IsValueType
            ? Comparer<T>.Default.Compare(value, previous) > 0
            : _comparer!.Compare(value, previous) > 0;
    }
}
