using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.Dangerous;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.FuzzySets;

/// <summary>
/// Allows search for strings in a set by a given prefix.
/// </summary>
public sealed class UnicodePrefixSet
{
    // The theory in this type is as follows:
    // A prefix of some strings will always sort before the strings it is a prefix of
    // but always sort after all the strings it is alphabetically after.
    // In other words: A non-match returns the index of first element that a is a prefix candidate.
    //
    // As such, we keep a sorted list of the strings and just perform a binary search:
    // - If we don't find an exact match, we know where to start checking for IsPrefix (at the insert index).
    //   We return every value until IsPrefix returns false for the first time.
    // - If we find an exact match, we just return that match. For now, we don't expect values in the set
    //   to be prefixes for other values, so this is sufficient.

    private readonly CompareInfo _compareInfo;
    private readonly StringComparer _comparer;
    private readonly ImmutableArray<string> _data;
    private readonly FrozenDictionary<PrefixSegment, PrefixRange> _prefixRanges;

    private UnicodePrefixSet(CompareInfo compareInfo, StringComparer comparer, ImmutableArray<string> data)
    {
        _compareInfo = compareInfo;
        _comparer = comparer;
        _data = data;

        _prefixRanges = CreatePrefixRangeDictionary();
    }

    /// <summary> Gets all values in this set, sorted. </summary>
    public ImmutableArray<string> Values => _data;

    /// <summary> The compare info this set uses. </summary>
    public CompareInfo CompareInfo => _compareInfo;

    /// <summary> Gets all strings with the given prefix in the set. </summary>
    /// <remarks> The results are lazily enumerated. </remarks>
    /// <param name="prefix"> The prefix to search for. </param>
    /// <returns> An enumerable of the prefixed elements. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="prefix"/> is null. </exception>
    public IEnumerable<string> Find(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        int len = Math.Min(prefix.Length, PrefixSegment.MaxContentLength);
        PrefixSegment segment = new(prefix.AsSpan(0, len));

        if (!_prefixRanges.TryGetValue(segment, out PrefixRange range))
            return Enumerable.Empty<string>();

        int index = _data.BinarySearch(range.Start, range.Length, prefix, _comparer);
        if (index < 0) index = ~index;

        return Enumerate(index, range.End, prefix);
    }

    private IEnumerable<string> Enumerate(int index, int end, string prefix)
    {
    More:
        var data = _data;
        if ((uint)index < (uint)end && (uint)index < (uint)data.Length)
        {
            string value = data[index++];
            if (_compareInfo.IsPrefix(value, prefix, FuzzySet.StringCompareOptions))
            {
                yield return value;
                goto More;
            }
        }
    }

    /// <summary> Creates a set from the given values using a specified compare info. </summary>
    /// <param name="compareInfo"> A compare info to use. </param>
    /// <param name="values"> The values in the set. </param>
    /// <returns> An equivalent prefix set. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="compareInfo"/> or <paramref name="values"/> is null. </exception>
    public static UnicodePrefixSet CreateFrom(CompareInfo compareInfo, IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(compareInfo);
        ArgumentNullException.ThrowIfNull(values);

        // This is intentionally and explicitly a copy to a new array.
        string[] array = values.ToArray();
        var comparer = compareInfo.GetStringComparer(FuzzySet.StringCompareOptions);

        Array.Sort(array, comparer);
        return new UnicodePrefixSet(compareInfo, comparer, ImmutableCollectionsMarshal.AsImmutableArray(array));
    }

    /// <summary> Creates a set from the given values using the compare info for the invariant culture. </summary>
    /// <param name="values"> The values in the set. </param>
    /// <returns> An equivalent prefix set. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="values"/> is null. </exception>
    public static UnicodePrefixSet CreateFromInvariant(IEnumerable<string> values)
    {
        return CreateFrom(CultureInfo.InvariantCulture.CompareInfo, values);
    }

    /// <summary> Gets all strings with the given prefix in the set of values. </summary>
    /// <remarks>
    /// This is less efficient than <see cref="CreateFrom"/> if the prefix set is used multiple times.
    /// The results are lazily enumerated.
    /// </remarks>
    /// <param name="compareInfo"> A compare info to use. </param>
    /// <param name="values"> The values in the set. </param>
    /// <param name="prefix"> The prefix to search for. </param>
    /// <returns> An enumerable of the prefixed elements. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="compareInfo"/> or <paramref name="values"/> or <paramref name="prefix"/> is null. </exception>
    public static IEnumerable<string> Find(CompareInfo compareInfo, IEnumerable<string> values, string prefix)
    {
        ArgumentNullException.ThrowIfNull(compareInfo);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(prefix);

        foreach (var value in values)
        {
            if (compareInfo.IsPrefix(value, prefix, FuzzySet.StringCompareOptions))
            {
                yield return value;
            }
        }
    }

    #region PrefixSegment and PrefixRange

    private FrozenDictionary<PrefixSegment, PrefixRange> CreatePrefixRangeDictionary()
    {
        // Going from the already sorted data, we can reduce the area we need to binary-search
        // by precomputing the supported range for initial small prefixes (here up to 4 characters).
        // Prefixes not present in the original set subsequently will be rejected without a search.

        var comparer = new PrefixSegmentComparer(_compareInfo);
        Dictionary<PrefixSegment, PrefixRange> result = new(comparer);
        HashSet<PrefixSegment> seen = new(comparer);

        var data = _data.AsSpan();
        foreach (var item in data)
        {
            int maxLen = Math.Min(item.Length, PrefixSegment.MaxContentLength);
            for (int i = 1; i <= maxLen; i++)
            {
                ReadOnlySpan<char> text = item.AsSpan(0, i);
                PrefixSegment segment = new(text);

                // If we have seen the segment already, no need to do anything.
                if (!seen.Add(segment)) continue;

                // Look for the start of the prefixed entries.
                int index = data.BinarySearch(new PrefixComparable(segment, _compareInfo));
                if (index < 0) index = ~index;

                // Iterate over the data until we hit a value which is not a prefix.
                // We necessarily hit at least one supported value.
                int start = index;
                while ((uint)index < (uint)data.Length)
                {
                    string value = data[index];
                    if (!_compareInfo.IsPrefix(value, text, FuzzySet.StringCompareOptions))
                        break;

                    index++;
                }

                // Store the supported range.
                result[segment] = new PrefixRange(start, index);
            }
        }

        return result.ToFrozenDictionary(comparer);
    }

    private readonly struct PrefixRange(int start, int end)
    {
        public int Start { get; } = start;
        public int End { get; } = end;
        public int Length => End - Start;
    }

    [SkipLocalsInit]
    private readonly struct PrefixSegment
    {
        public const int MaxContentLength = sizeof(ulong) / sizeof(char);

        private readonly ulong _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PrefixSegment(ReadOnlySpan<char> buffer)
        {
            Debug.Assert(buffer.Length <= MaxContentLength);

            _value = default;
            buffer.CopyTo(DangerousSpan.CreateFromBuffer<ulong, char>(ref _value));
        }

        public ulong Value => _value;

        [UnscopedRef]
        public ReadOnlySpan<char> Text => DangerousSpan.CreateFromReadOnlyBuffer<ulong, char>(in _value).NullTerminate();
        public override string ToString() => new string(Text);
    }

    private readonly struct PrefixComparable(PrefixSegment segment, CompareInfo compareInfo) : IComparable<string>
    {
        public int CompareTo(string? other)
        {
            if (other == null) return -1;
            return compareInfo.Compare(segment.Text, other, FuzzySet.StringCompareOptions);
        }
    }

    private sealed class PrefixSegmentComparer(CompareInfo compareInfo) : IEqualityComparer<PrefixSegment>
    {
        public bool Equals(PrefixSegment x, PrefixSegment y)
        {
            if (x.Value == y.Value) return true;
            return compareInfo.Compare(x.Text, y.Text, FuzzySet.StringCompareOptions) == 0;
        }

        public int GetHashCode(PrefixSegment obj)
        {
            return compareInfo.GetHashCode(obj.Text, FuzzySet.StringCompareOptions);
        }
    }

    #endregion
}
