using System;
using System.Collections.Generic;
using CelestiaCS.Lib.Collections;

namespace CelestiaCS.Lib.Random;

/// <summary>
/// Provides extension methods for <see cref="IRng"/> instances that allow shuffling collections.
/// </summary>
public static class RngShuffleExtensions
{
    /// <summary>
    /// Returns a new enumerable, based on the old one, but with its items shuffled randomly.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="rng"> The random to use. </param>
    /// <param name="source"> The source collection. </param>
    /// <returns> A shuffled collection with the same items. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public static IEnumerable<T> Shuffled<T>(this IRng rng, IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(source);

        ValueList<T> list = default;
        list.AddRange(source);
        rng.Shuffle(list.AsSpan());
        return list.ToEnumerable();
    }

    /// <summary>
    /// Randomly shuffles the elements in the span.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="rng"> The random to use. </param>
    /// <param name="source"> The span to be shuffled. </param>
    public static void Shuffle<T>(this IRng rng, Span<T> source)
    {
        ArgumentNullException.ThrowIfNull(rng);

        for (int i = 0; i < source.Length - 1; i++)
        {
            int f = rng.Int(i, source.Length);
            (source[f], source[i]) = (source[i], source[f]);
        }
    }
}
