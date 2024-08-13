using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using CelestiaCS.Lib;

// Keep this namespace as this!
// We want the methods below to always replace the BCL's LINQ methods if possible.
namespace System.Linq;

/// <summary>
/// Like <see cref="ImmutableArrayExtensions"/>, provides LINQ extension method overrides that avoid boxing <see cref="ImmutableArray{T}"/> and throw sooner.
/// </summary>
public static class ImmutableArrayCelestiaExtensions
{
    #region SelectMany

    /// <inheritdoc cref="Enumerable.SelectMany{TSource, TResult}(IEnumerable{TSource}, Func{TSource, IEnumerable{TResult}})"/>
    public static IEnumerable<TResult> SelectMany<TSource, TResult>(this ImmutableArray<TSource> source, Func<TSource, IEnumerable<TResult>> selector) => FLinq(source).SelectMany(selector);

    /// <inheritdoc cref="Enumerable.SelectMany{TSource, TResult}(IEnumerable{TSource}, Func{TSource, IEnumerable{TResult}})"/>
    public static IEnumerable<TResult> SelectMany<TSource, TResult>(this ImmutableArray<TSource> source, Func<TSource, ImmutableArray<TResult>> selector) => FLinq(source).SelectMany(selector.SelectManyFLinq);

    /// <inheritdoc cref="Enumerable.SelectMany{TSource, TResult}(IEnumerable{TSource}, Func{TSource, IEnumerable{TResult}})"/>
    public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, ImmutableArray<TResult>> selector) => source.SelectMany(selector.SelectManyFLinq);

    // Extension method used to minimize allocations for some SelectMany overloads.
    private static IEnumerable<TResult> SelectManyFLinq<TSource, TResult>(this Func<TSource, ImmutableArray<TResult>> selector, TSource source) => FLinq(selector(source));

    #endregion

    #region Intersect

    /// <inheritdoc cref="Enumerable.Intersect{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
    public static IEnumerable<TSource> Intersect<TSource>(this ImmutableArray<TSource> first, IEnumerable<TSource> second) => FLinq(first).Intersect(second);

    /// <inheritdoc cref="Enumerable.Intersect{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
    public static IEnumerable<TSource> Intersect<TSource>(this ImmutableArray<TSource> first, ImmutableArray<TSource> second) => FLinq(first).Intersect(FLinq(second));

    /// <inheritdoc cref="Enumerable.Intersect{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
    public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, ImmutableArray<TSource> second) => first.Intersect(FLinq(second));

    /// <inheritdoc cref="Enumerable.Intersect{TSource}(IEnumerable{TSource}, IEnumerable{TSource}, IEqualityComparer{TSource}?)"/>
    public static IEnumerable<TSource> Intersect<TSource>(this ImmutableArray<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer) => FLinq(first).Intersect(second, comparer);

    /// <inheritdoc cref="Enumerable.Intersect{TSource}(IEnumerable{TSource}, IEnumerable{TSource}, IEqualityComparer{TSource}?)"/>
    public static IEnumerable<TSource> Intersect<TSource>(this ImmutableArray<TSource> first, ImmutableArray<TSource> second, IEqualityComparer<TSource>? comparer) => FLinq(first).Intersect(FLinq(second), comparer);

    /// <inheritdoc cref="Enumerable.Intersect{TSource}(IEnumerable{TSource}, IEnumerable{TSource}, IEqualityComparer{TSource}?)"/>
    public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, ImmutableArray<TSource> second, IEqualityComparer<TSource>? comparer) => first.Intersect(FLinq(second), comparer);

    #endregion

    #region Except

    /// <inheritdoc cref="Enumerable.Except{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
    public static IEnumerable<TSource> Except<TSource>(this ImmutableArray<TSource> first, IEnumerable<TSource> second) => FLinq(first).Except(second);

    /// <inheritdoc cref="Enumerable.Except{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
    public static IEnumerable<TSource> Except<TSource>(this ImmutableArray<TSource> first, ImmutableArray<TSource> second) => FLinq(first).Except(FLinq(second));

    /// <inheritdoc cref="Enumerable.Except{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>
    public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, ImmutableArray<TSource> second) => first.Except(FLinq(second));

    /// <inheritdoc cref="Enumerable.Except{TSource}(IEnumerable{TSource}, IEnumerable{TSource}, IEqualityComparer{TSource}?)"/>
    public static IEnumerable<TSource> Except<TSource>(this ImmutableArray<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer) => FLinq(first).Except(second, comparer);

    /// <inheritdoc cref="Enumerable.Except{TSource}(IEnumerable{TSource}, IEnumerable{TSource}, IEqualityComparer{TSource}?)"/>
    public static IEnumerable<TSource> Except<TSource>(this ImmutableArray<TSource> first, ImmutableArray<TSource> second, IEqualityComparer<TSource>? comparer) => FLinq(first).Except(FLinq(second), comparer);

    /// <inheritdoc cref="Enumerable.Except{TSource}(IEnumerable{TSource}, IEnumerable{TSource}, IEqualityComparer{TSource}?)"/>
    public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, ImmutableArray<TSource> second, IEqualityComparer<TSource>? comparer) => first.Except(FLinq(second), comparer);

    #endregion

    #region Skip/Take

    /// <inheritdoc cref="Enumerable.Skip{TSource}(IEnumerable{TSource}, int)"/>
    public static IEnumerable<TSource> Skip<TSource>(this ImmutableArray<TSource> source, int count) => FLinq(source).Skip(count);

    /// <inheritdoc cref="Enumerable.Take{TSource}(IEnumerable{TSource}, int)"/>
    public static IEnumerable<TSource> Take<TSource>(this ImmutableArray<TSource> source, int count) => FLinq(source).Take(count);

    /// <inheritdoc cref="Enumerable.Take{TSource}(IEnumerable{TSource}, Range)"/>
    public static IEnumerable<TSource> Take<TSource>(this ImmutableArray<TSource> source, Range range) => FLinq(source).Take(range);

    #endregion

    #region Average

    /// <inheritdoc cref="Enumerable.Average{TSource}(IEnumerable{TSource}, Func{TSource, int})"/>
    public static double Average<TSource>(this ImmutableArray<TSource> source, Func<TSource, int> selector) => FLinq(source).Average(selector);

    /// <inheritdoc cref="Enumerable.Average{TSource}(IEnumerable{TSource}, Func{TSource, int?})"/>
    public static double? Average<TSource>(this ImmutableArray<TSource> source, Func<TSource, int?> selector) => FLinq(source).Average(selector);

    /// <inheritdoc cref="Enumerable.Average{TSource}(IEnumerable{TSource}, Func{TSource, long})"/>
    public static double Average<TSource>(this ImmutableArray<TSource> source, Func<TSource, long> selector) => FLinq(source).Average(selector);

    /// <inheritdoc cref="Enumerable.Average{TSource}(IEnumerable{TSource}, Func{TSource, long?})"/>
    public static double? Average<TSource>(this ImmutableArray<TSource> source, Func<TSource, long?> selector) => FLinq(source).Average(selector);

    /// <inheritdoc cref="Enumerable.Average{TSource}(IEnumerable{TSource}, Func{TSource, float})"/>
    public static float Average<TSource>(this ImmutableArray<TSource> source, Func<TSource, float> selector) => FLinq(source).Average(selector);

    /// <inheritdoc cref="Enumerable.Average{TSource}(IEnumerable{TSource}, Func{TSource, float?})"/>
    public static float? Average<TSource>(this ImmutableArray<TSource> source, Func<TSource, float?> selector) => FLinq(source).Average(selector);

    /// <inheritdoc cref="Enumerable.Average{TSource}(IEnumerable{TSource}, Func{TSource, double})"/>
    public static double Average<TSource>(this ImmutableArray<TSource> source, Func<TSource, double> selector) => FLinq(source).Average(selector);

    /// <inheritdoc cref="Enumerable.Average{TSource}(IEnumerable{TSource}, Func{TSource, double?})"/>
    public static double? Average<TSource>(this ImmutableArray<TSource> source, Func<TSource, double?> selector) => FLinq(source).Average(selector);

    /// <inheritdoc cref="Enumerable.Average{TSource}(IEnumerable{TSource}, Func{TSource, decimal})"/>
    public static decimal Average<TSource>(this ImmutableArray<TSource> source, Func<TSource, decimal> selector) => FLinq(source).Average(selector);

    /// <inheritdoc cref="Enumerable.Average{TSource}(IEnumerable{TSource}, Func{TSource, decimal?})"/>
    public static decimal? Average<TSource>(this ImmutableArray<TSource> source, Func<TSource, decimal?> selector) => FLinq(source).Average(selector);

    #endregion

    #region Min/Max

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource})"/>
    public static TSource? Min<TSource>(this ImmutableArray<TSource> source) => FLinq(source).Min();

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource},IComparer{TSource})"/>
    public static TSource? Min<TSource>(this ImmutableArray<TSource> source, IComparer<TSource>? comparer) => FLinq(source).Min(comparer);

    /// <inheritdoc cref="Enumerable.Min{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})"/>
    public static TResult? Min<TSource, TResult>(this ImmutableArray<TSource> source, Func<TSource, TResult> selector) => FLinq(source).Min(selector);

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource}, Func{TSource, int})"/>
    public static int Min<TSource>(this ImmutableArray<TSource> source, Func<TSource, int> selector) => FLinq(source).Min(selector);

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource}, Func{TSource, int?})"/>
    public static int? Min<TSource>(this ImmutableArray<TSource> source, Func<TSource, int?> selector) => FLinq(source).Min(selector);

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource}, Func{TSource, long})"/>
    public static long Min<TSource>(this ImmutableArray<TSource> source, Func<TSource, long> selector) => FLinq(source).Min(selector);

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource}, Func{TSource, long?})"/>
    public static long? Min<TSource>(this ImmutableArray<TSource> source, Func<TSource, long?> selector) => FLinq(source).Min(selector);

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource}, Func{TSource, float})"/>
    public static float Min<TSource>(this ImmutableArray<TSource> source, Func<TSource, float> selector) => FLinq(source).Min(selector);

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource}, Func{TSource, float?})"/>
    public static float? Min<TSource>(this ImmutableArray<TSource> source, Func<TSource, float?> selector) => FLinq(source).Min(selector);

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource}, Func{TSource, double})"/>
    public static double Min<TSource>(this ImmutableArray<TSource> source, Func<TSource, double> selector) => FLinq(source).Min(selector);

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource}, Func{TSource, double?})"/>
    public static double? Min<TSource>(this ImmutableArray<TSource> source, Func<TSource, double?> selector) => FLinq(source).Min(selector);

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource}, Func{TSource, decimal})"/>
    public static decimal Min<TSource>(this ImmutableArray<TSource> source, Func<TSource, decimal> selector) => FLinq(source).Min(selector);

    /// <inheritdoc cref="Enumerable.Min{TSource}(IEnumerable{TSource}, Func{TSource, decimal?})"/>
    public static decimal? Min<TSource>(this ImmutableArray<TSource> source, Func<TSource, decimal?> selector) => FLinq(source).Min(selector);

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource})"/>
    public static TSource? Max<TSource>(this ImmutableArray<TSource> source) => FLinq(source).Max();

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource},IComparer{TSource})"/>
    public static TSource? Max<TSource>(this ImmutableArray<TSource> source, IComparer<TSource>? comparer) => FLinq(source).Max(comparer);

    /// <inheritdoc cref="Enumerable.Max{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})"/>
    public static TResult? Max<TSource, TResult>(this ImmutableArray<TSource> source, Func<TSource, TResult> selector) => FLinq(source).Max(selector);

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource}, Func{TSource, int})"/>
    public static int Max<TSource>(this ImmutableArray<TSource> source, Func<TSource, int> selector) => FLinq(source).Max(selector);

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource}, Func{TSource, int?})"/>
    public static int? Max<TSource>(this ImmutableArray<TSource> source, Func<TSource, int?> selector) => FLinq(source).Max(selector);

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource}, Func{TSource, long})"/>
    public static long Max<TSource>(this ImmutableArray<TSource> source, Func<TSource, long> selector) => FLinq(source).Max(selector);

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource}, Func{TSource, long?})"/>
    public static long? Max<TSource>(this ImmutableArray<TSource> source, Func<TSource, long?> selector) => FLinq(source).Max(selector);

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource}, Func{TSource, float})"/>
    public static float Max<TSource>(this ImmutableArray<TSource> source, Func<TSource, float> selector) => FLinq(source).Max(selector);

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource}, Func{TSource, float?})"/>
    public static float? Max<TSource>(this ImmutableArray<TSource> source, Func<TSource, float?> selector) => FLinq(source).Max(selector);

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource}, Func{TSource, double})"/>
    public static double Max<TSource>(this ImmutableArray<TSource> source, Func<TSource, double> selector) => FLinq(source).Max(selector);

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource}, Func{TSource, double?})"/>
    public static double? Max<TSource>(this ImmutableArray<TSource> source, Func<TSource, double?> selector) => FLinq(source).Max(selector);

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource}, Func{TSource, decimal})"/>
    public static decimal Max<TSource>(this ImmutableArray<TSource> source, Func<TSource, decimal> selector) => FLinq(source).Max(selector);

    /// <inheritdoc cref="Enumerable.Max{TSource}(IEnumerable{TSource}, Func{TSource, decimal?})"/>
    public static decimal? Max<TSource>(this ImmutableArray<TSource> source, Func<TSource, decimal?> selector) => FLinq(source).Max(selector);

    #endregion

    #region Sum

    /// <inheritdoc cref="Enumerable.Sum(IEnumerable{int})"/>
    public static int Sum(this ImmutableArray<int> source) => FLinq(source).Sum();

    /// <inheritdoc cref="Enumerable.Sum(IEnumerable{int?})"/>
    public static int? Sum(this ImmutableArray<int?> source) => FLinq(source).Sum();

    /// <inheritdoc cref="Enumerable.Sum(IEnumerable{long})"/>
    public static long Sum(this ImmutableArray<long> source) => FLinq(source).Sum();

    /// <inheritdoc cref="Enumerable.Sum(IEnumerable{long?})"/>
    public static long? Sum(this ImmutableArray<long?> source) => FLinq(source).Sum();

    /// <inheritdoc cref="Enumerable.Sum(IEnumerable{float})"/>
    public static float Sum(this ImmutableArray<float> source) => FLinq(source).Sum();

    /// <inheritdoc cref="Enumerable.Sum(IEnumerable{float?})"/>
    public static float? Sum(this ImmutableArray<float?> source) => FLinq(source).Sum();

    /// <inheritdoc cref="Enumerable.Sum(IEnumerable{double})"/>
    public static double Sum(this ImmutableArray<double> source) => FLinq(source).Sum();

    /// <inheritdoc cref="Enumerable.Sum(IEnumerable{double?})"/>
    public static double? Sum(this ImmutableArray<double?> source) => FLinq(source).Sum();

    /// <inheritdoc cref="Enumerable.Sum(IEnumerable{decimal})"/>
    public static decimal Sum(this ImmutableArray<decimal> source) => FLinq(source).Sum();

    /// <inheritdoc cref="Enumerable.Sum(IEnumerable{decimal?})"/>
    public static decimal? Sum(this ImmutableArray<decimal?> source) => FLinq(source).Sum();

    /// <inheritdoc cref="Enumerable.Sum{TSource}(IEnumerable{TSource}, Func{TSource, int})"/>
    public static int Sum<TSource>(this ImmutableArray<TSource> source, Func<TSource, int> selector) => FLinq(source).Sum(selector);

    /// <inheritdoc cref="Enumerable.Sum{TSource}(IEnumerable{TSource}, Func{TSource, int?})"/>
    public static int? Sum<TSource>(this ImmutableArray<TSource> source, Func<TSource, int?> selector) => FLinq(source).Sum(selector);

    /// <inheritdoc cref="Enumerable.Sum{TSource}(IEnumerable{TSource}, Func{TSource, long})"/>
    public static long Sum<TSource>(this ImmutableArray<TSource> source, Func<TSource, long> selector) => FLinq(source).Sum(selector);

    /// <inheritdoc cref="Enumerable.Sum{TSource}(IEnumerable{TSource}, Func{TSource, long?})"/>
    public static long? Sum<TSource>(this ImmutableArray<TSource> source, Func<TSource, long?> selector) => FLinq(source).Sum(selector);

    /// <inheritdoc cref="Enumerable.Sum{TSource}(IEnumerable{TSource}, Func{TSource, float})"/>
    public static float Sum<TSource>(this ImmutableArray<TSource> source, Func<TSource, float> selector) => FLinq(source).Sum(selector);

    /// <inheritdoc cref="Enumerable.Sum{TSource}(IEnumerable{TSource}, Func{TSource, float?})"/>
    public static float? Sum<TSource>(this ImmutableArray<TSource> source, Func<TSource, float?> selector) => FLinq(source).Sum(selector);

    /// <inheritdoc cref="Enumerable.Sum{TSource}(IEnumerable{TSource}, Func{TSource, double})"/>
    public static double Sum<TSource>(this ImmutableArray<TSource> source, Func<TSource, double> selector) => FLinq(source).Sum(selector);

    /// <inheritdoc cref="Enumerable.Sum{TSource}(IEnumerable{TSource}, Func{TSource, double?})"/>
    public static double? Sum<TSource>(this ImmutableArray<TSource> source, Func<TSource, double?> selector) => FLinq(source).Sum(selector);

    /// <inheritdoc cref="Enumerable.Sum{TSource}(IEnumerable{TSource}, Func{TSource, decimal})"/>
    public static decimal Sum<TSource>(this ImmutableArray<TSource> source, Func<TSource, decimal> selector) => FLinq(source).Sum(selector);

    /// <inheritdoc cref="Enumerable.Sum{TSource}(IEnumerable{TSource}, Func{TSource, decimal?})"/>
    public static decimal? Sum<TSource>(this ImmutableArray<TSource> source, Func<TSource, decimal?> selector) => FLinq(source).Sum(selector);

    #endregion

    private static IEnumerable<T> FLinq<T>(ImmutableArray<T> source)
    {
        var array = ImmutableCollectionsMarshal.AsArray(source);
        if (array == null) ThrowDefaultImmutableArray();
        return array;
    }

    [DoesNotReturn]
    private static void ThrowDefaultImmutableArray()
    {
        VarHelper.Cast<IEnumerable<int>>(default(ImmutableArray<int>)).GetEnumerator();
        ThrowHelper.InvalidOperation();
    }
}
