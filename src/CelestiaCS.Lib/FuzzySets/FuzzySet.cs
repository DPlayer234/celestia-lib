using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.Collections;
using CelestiaCS.Lib.Linq;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.FuzzySets;

/*
Adapted from https://glench.github.io/fuzzyset.js/
At this point basically every part has been rewritten for better performance.
Sets default gram sizes to 4-6
*/

/// <summary>
/// A fuzzy set where every string is normalized to lower case and just letters and numbers.
/// </summary>
/// <typeparam name="TMeta"> The type of the data to associate. </typeparam>
[SkipLocalsInit]
public sealed class FuzzySet<TMeta>
{
    // If we find more than this amount of results in one access, we exit to avoid
    // spending too much memory on the results or too much time on sorting after.
    private const int ResultSizeLimit = 32;

    // Static fields in a generic class aren't as fast, and we have few instances,
    // so we may as well allocate this per instance.
    private readonly Comparison<FuzzyResult<TMeta>> _comparer = (a, b) =>
    {
        if (a.Score < b.Score) return 1;
        if (a.Score > b.Score) return -1;
        return 0;
    };

    private readonly int _gramSizeLower;
    private readonly int _gramSizeUpper;
    private readonly CultureInfo _cultureInfo;
    private readonly Dictionary<string, TMeta> _exactDict;
    private readonly Dictionary<FuzzySegment, ValueList<int>> _matchDict = [];
    private ValueList<TMeta> _exactList;
    private ValueList<FuzzySlice> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="FuzzySet{TMeta}"/> class.
    /// </summary>
    /// <param name="gramSizeLower"> The lower gram size to use. </param>
    /// <param name="gramSizeUpper"> The upper gram size to use. </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="gramSizeUpper"/> or <paramref name="gramSizeLower"/> are negative or zero, or
    /// <paramref name="gramSizeUpper"/> is greater than 8, or
    /// <paramref name="gramSizeLower"/> is greater than <paramref name="gramSizeUpper"/>.
    /// </exception>
    public FuzzySet(int gramSizeLower = 4, int gramSizeUpper = 6, CultureInfo? cultureInfo = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gramSizeLower);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gramSizeUpper);

        if (gramSizeUpper > FuzzySegment.MaxContentLength)
            ThrowHelper.ArgumentOutOfRange(nameof(gramSizeUpper), gramSizeUpper, "Gram Size must be at most 8.");
        if (gramSizeLower > gramSizeUpper)
            ThrowHelper.ArgumentOutOfRange(nameof(gramSizeLower), gramSizeLower, "Gram Size lower limit must be at most the upper limit.");

        cultureInfo ??= CultureInfo.InvariantCulture;

        _gramSizeLower = gramSizeLower;
        _gramSizeUpper = gramSizeUpper;
        _cultureInfo = cultureInfo;

        _exactDict = new(cultureInfo.CompareInfo.GetStringComparer(FuzzySet.StringCompareOptions));
    }

    /// <summary> Gets the amount of items stored in this set. </summary>
    public int Count => _items.Count;

    /// <summary> Gets the values stored in this set. </summary>
    public IEnumerable<TMeta> Values => _exactList.ToEnumerable();

    /// <summary>
    /// Adds a value to the set.
    /// </summary>
    /// <param name="value"> The value to add. </param>
    /// <param name="meta"> The data to associate. </param>
    /// <returns> If the value was added. Also returns <see langword="false"/> if the value was already present. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="value"/> is null. </exception>
    public bool Add(string value, TMeta meta)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (_exactDict.TryGetValue(value, out TMeta? present))
        {
            // We can't return the buffer before here as normValue is used still.
            if (EqualityComparer<TMeta>.Default.Equals(meta, present))
            {
                return false;
            }

            string normValueAsString = FuzzySetHelper.NormalizedToString(value, _cultureInfo);
            ThrowHelper.InvalidOperation($"The same value was already present, but it had different data associated: {normValueAsString}");
        }

        // This slice needs be returned before each return/throw statement
        FuzzySlice normValue = FuzzySetHelper.RentNormalized(value, _cultureInfo);

        // We can skip storing grams if the input length is at most the lower gram size.
        // In that case, the exact set would already have all the information to store.
        int gramSizeLower = _gramSizeLower;
        if (normValue.Length > gramSizeLower)
        {
            // Limit gram sizes to avoid unnecessary grams
            // If upper < lower, then there are no grams to search and only exact-sets will match
            int gramSizeUpper = Math.Min(normValue.Length, _gramSizeUpper);

            for (int i = gramSizeLower; i <= gramSizeUpper; i++)
            {
                AddGramsOf(normValue, i);
            }
        }

        // We need to allocate the normalized form to be stored and return the rented buffer
        var normValueCopy = normValue.Copy();
        FuzzySetHelper.Return(normValue);

        _items.Add(normValueCopy);
        _exactList.Add(meta);
        _exactDict.Add(value, meta);

        return true;
    }

    /// <summary>
    /// Gets all data associated with similar values above <paramref name="minMatchScore"/>.
    /// </summary>
    /// <param name="value"> The value to search for. </param>
    /// <param name="minMatchScore"> The minimum matching score. </param>
    /// <returns> All found associated data values. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="value"/> is null. </exception>
    public IEnumerable<FuzzyResult<TMeta>> Get(string value, double minMatchScore = 0.33)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (_exactDict.TryGetValue(value, out var meta))
        {
            return FuzzyResult<TMeta>.Exact(meta);
        }

        // This slice needs be returned before each return statement
        FuzzySlice normValue = FuzzySetHelper.RentNormalized(value, _cultureInfo);

        // We can skip this search if the input length is at most the lower gram size.
        // In that case, the exact set would already match.
        int gramSizeLower = _gramSizeLower;
        if (normValue.Length > gramSizeLower)
        {
            // Limit gram sizes to avoid unnecessary grams.
            // If upper < lower, then there are no grams to search
            int gramSizeUpper = Math.Min(normValue.Length, _gramSizeUpper);

            // start with high gram size and if there are no results, go to lower gram sizes
            for (int gramSize = gramSizeUpper; gramSize >= gramSizeLower; --gramSize)
            {
                var results = GetWithGramSize(normValue, gramSize, minMatchScore);
                if (results.Any())
                {
                    FuzzySetHelper.Return(normValue);

                    results.AsSpan().Sort(_comparer);
                    return results.ToEnumerable();
                }
            }
        }

        FuzzySetHelper.Return(normValue);
        return Enumerable.Empty<FuzzyResult<TMeta>>();
    }

    /// <summary>
    /// Trims the internal lists down to the required size.
    /// </summary>
    public void TrimLists()
    {
        _matchDict.TrimExcess();
        foreach (var key in _matchDict.Keys)
        {
            ref var list = ref CollectionsMarshal.GetValueRefOrNullRef(_matchDict, key);
            Debug.Assert(!Unsafe.IsNullRef(ref list));

            list.TrimExcess();
        }

        _items.TrimExcess();
        _exactList.TrimExcess();
    }

    private void AddGramsOf(FuzzySlice value, int gramSize)
    {
        // This needs to be called before _items and
        // the 'exact' collections are augmented.
        int index = _items.Count;

        GramsIterator iterator = new(value, gramSize);
        while (iterator.MoveNext())
        {
            _matchDict.AddToValue(iterator.Current, index);
        }
    }

    private ValueList<FuzzyResult<TMeta>> GetWithGramSize(FuzzySlice normValue, int gramSize, double minMatchScore)
    {
        var matchRent = SpanRenter<int>.UseStackOrRent(normValue.Length, stackalloc int[FuzzySetHelper.MaxStackAllocSize]);
        Span<int> matchSet = matchRent.Span;
        int matchCount = 0;

        ValueList<FuzzyResult<TMeta>> results = default;
        var normValueSpan = normValue.AsSpan();

        GramsIterator iterator = new(normValue, gramSize);
        while (iterator.MoveNext())
        {
            ref var matchDictEntry = ref CollectionsMarshal.GetValueRefOrNullRef(_matchDict, iterator.Current);
            if (Unsafe.IsNullRef(ref matchDictEntry)) continue;

            foreach (int matchIndex in matchDictEntry)
            {
                if (matchSet[..matchCount].Contains(matchIndex))
                    continue;

                matchSet[matchCount++] = matchIndex;
                var normRes = _items[matchIndex].AsSpan();
                if (FuzzySetHelper.AreSimilar(normRes, normValueSpan, minMatchScore, out double score))
                {
                    results.Add(new(score, _exactList[matchIndex]));

                    if (results.Count == ResultSizeLimit)
                        goto EarlyExit;
                }
            }
        }

    EarlyExit:
        matchRent.Return();
        return results;
    }
}

/// <summary>
/// Provides extensions for fuzzy sets and to create them from other data.
/// </summary>
public static class FuzzySet
{
    /// <summary>
    /// Provides compare options that can be used with a culture's <see cref="CompareInfo"/> to compare
    /// strings in a way that is equivalent to how a fuzzy set treats strings.
    /// </summary>
    public const CompareOptions StringCompareOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType;

    /// <summary>
    /// Creates a new <see cref="FuzzySet{TMeta}"/>, mapping data from another source into its values and associated data.
    /// </summary>
    /// <typeparam name="T"> The source data type. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="toValue"> A delegate that maps a source item into its value. </param>
    /// <returns> A trimmed fuzzy set with all the data. </returns>
    public static FuzzySet<T> ToFuzzySet<T>(this IEnumerable<T> source, Func<T, string> toValue, CultureInfo? cultureInfo = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(toValue);

        var fuzzySet = new FuzzySet<T>(cultureInfo: cultureInfo);

        foreach (var item in source)
        {
            fuzzySet.Add(toValue(item), item);
        }

        fuzzySet.TrimLists();
        return fuzzySet;
    }

    /// <summary>
    /// Creates a new <see cref="FuzzySet{TMeta}"/>, mapping data from another source into its values and associated data.
    /// </summary>
    /// <typeparam name="TSource"> The source data type. </typeparam>
    /// <typeparam name="TMeta"> The fuzzy-set associated data. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="toValue"> A delegate that maps a source item into its value. </param>
    /// <param name="toMeta"> A delegate that maps a source item into its associated data. </param>
    /// <returns> A trimmed fuzzy set with all the data. </returns>
    public static FuzzySet<TMeta> ToFuzzySet<TSource, TMeta>(this IEnumerable<TSource> source, Func<TSource, string> toValue, Func<TSource, TMeta> toMeta, CultureInfo? cultureInfo = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(toValue);
        ArgumentNullException.ThrowIfNull(toMeta);

        var fuzzySet = new FuzzySet<TMeta>(cultureInfo: cultureInfo);

        foreach (var item in source)
        {
            fuzzySet.Add(toValue(item), toMeta(item));
        }

        fuzzySet.TrimLists();
        return fuzzySet;
    }
}
