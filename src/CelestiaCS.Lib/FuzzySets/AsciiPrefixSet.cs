using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CelestiaCS.Lib.Linq;

namespace CelestiaCS.Lib.FuzzySets;

/// <summary>
/// Stores and holds normalized strings to be accessed by a prefix.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class AsciiPrefixSet
{
    private PrefixSetBucket _root;

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="AsciiPrefixSet"/> class.
    /// </summary>
    public AsciiPrefixSet() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsciiPrefixSet"/> class with the given values.
    /// </summary>
    /// <param name="values"> The values to add. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="values"/> is null. </exception>
    public AsciiPrefixSet(IEnumerable<string> values)
    {
        Add(values);
    }

    private string DebuggerDisplay
    {
        get
        {
            var (b, r) = _root.CountAll();
            return $"PrefixSet {{ Arrays = {b}, Results = {r} }}";
        }
    }

    /// <summary>
    /// Adds a value to the set.
    /// </summary>
    /// <param name="value"> The value to add. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="value"/> is null. </exception>
    public void Add(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _root.Add(value, value, 0);
    }

    /// <summary>
    /// Adds a set of values to this set.
    /// </summary>
    /// <param name="values"> The values to add. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="values"/> is null. </exception>
    public void Add(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (string value in values)
        {
            Add(value);
        }
    }

    /// <summary>
    /// Gets all values in the set that begin with the specified prefix.
    /// </summary>
    /// <remarks>
    /// The result is lazily enumerated. It is best to limit the results taken from here and
    /// not return everything to the user.
    /// </remarks>
    /// <param name="prefix"> The prefix. </param>
    /// <returns> The found values in the set. </returns>
    public IEnumerable<string> Get(ReadOnlySpan<char> prefix)
    {
        return _root.Get(prefix) ?? Enumerable.Empty<string>();
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    private struct PrefixSetBucket
    {
        private const int BucketSize = 26 + 10;

        private PrefixSetBucket[]? _buckets;
        private string? _result;

        private readonly string DebuggerDisplay
        {
            get
            {
                var (b, r) = CountAll();
                return $"PrefixSetBucket {{ Result = {_result ?? "<None>"}, Arrays = {b}, Results = {r} }}";
            }
        }

        internal void Add(string strValue, ReadOnlySpan<char> value, int pos)
        {
            if (pos < value.Length)
            {
                _buckets ??= new PrefixSetBucket[BucketSize];

                int bucket = Seek(value, ref pos);
                if (bucket != -1)
                {
                    _buckets[bucket].Add(strValue, value, pos);
                }
            }
            else
            {
                _result ??= strValue;
            }
        }

        internal readonly IEnumerable<string>? Get(ReadOnlySpan<char> prefix)
        {
            int pos = 0;
            int bucket = Seek(prefix, ref pos);
            return bucket == -1 ? GetNestedResults()
                : pos <= prefix.Length ? _buckets?[bucket].Get(prefix[pos..])
                : null;
        }

        internal readonly (int arrays, int results) CountAll()
        {
            int arrays = 0, results = 0;
            if (_buckets != null)
            {
                arrays += 1;
                foreach (var bucket in _buckets)
                {
                    var (b, r) = bucket.CountAll();
                    arrays += b;
                    results += r;
                }
            }

            if (_result != null)
            {
                results += 1;
            }

            return (arrays, results);
        }

        private readonly IEnumerable<string> GetNestedResults()
        {
            var buckets = _buckets;
            var result = _result;
            if (buckets != null)
            {
                var nested = buckets.SelectMany(x => x.GetNestedResults());
                return result != null ? nested.Prepend(result) : nested;
            }
            else if (result != null)
            {
                return EnumerableEx.Once(result);
            }

            return Enumerable.Empty<string>();
        }

        private readonly int Seek(ReadOnlySpan<char> value, ref int pos)
        {
            int index = -1;
            for (int i = pos; i < value.Length; i++)
            {
                char a = value[i];
                if (a is >= 'A' and <= 'Z')
                    index = a - 'A' + 10;
                else if (a is >= 'a' and <= 'z')
                    index = a - 'a' + 10;
                else if (a is >= '0' and <= '9')
                    index = a - '0';

                if (index != -1)
                {
                    pos = i + 1;
                    return index;
                }
            }

            return -1;
        }
    }
}
