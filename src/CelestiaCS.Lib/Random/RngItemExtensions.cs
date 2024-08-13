using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.Random;

/// <summary>
/// Provides extensions methods for <see cref="IRng"/> instances to select random collection items.
/// </summary>
public static class RngItemExtensions
{
    /// <summary>
    /// Returns a random item within an array. Every element has the same chance to be returned.
    /// </summary>
    /// <typeparam name="T"> The type of items in the array. </typeparam>
    /// <param name="rng"> The source randomness. </param>
    /// <param name="array"> The array to pick an item from. </param>
    /// <returns> A random item or <see langword="default"/> if the array is empty. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="array"/> is null. </exception>
    public static T? Item<T>(this IRng rng, T[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        return Item(rng, array.AsReadOnlySpan());
    }

    /// <summary>
    /// Returns a random item within an array. Every element has the same chance to be returned.
    /// </summary>
    /// <typeparam name="T"> The type of items in the array. </typeparam>
    /// <param name="rng"> The source randomness. </param>
    /// <param name="array"> The array to pick an item from. </param>
    /// <returns> A random item or <see langword="default"/> if the array is empty. </returns>
    public static T? Item<T>(this IRng rng, ImmutableArray<T> array) => Item(rng, array.AsSpan());

    /// <summary>
    /// Returns a random item within a span. Every element has the same chance to be returned.
    /// </summary>
    /// <typeparam name="T"> The type of items in the span. </typeparam>
    /// <param name="rng"> The source randomness. </param>
    /// <param name="span"> The span to pick an item from. </param>
    /// <returns> A random item or <see langword="default"/> if the span is empty. </returns>
    public static T? Item<T>(this IRng rng, ReadOnlySpan<T> span)
    {
        ArgumentNullException.ThrowIfNull(rng);

        int length = span.Length;
        return length switch
        {
            0 => default,
            1 => span[0],
            _ => span[rng.Int(length)]
        };
    }

    /// <summary>
    /// Returns a random item within a list. Every element has the same chance to be returned.
    /// </summary>
    /// <typeparam name="T"> The type of items in the list. </typeparam>
    /// <param name="rng"> The source randomness. </param>
    /// <param name="list"> The list to pick an item from. </param>
    /// <returns> A random item or <see langword="default"/> if the list is empty. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="list"/> is null. </exception>
    public static T? Item<T>(this IRng rng, IReadOnlyList<T> list)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(list);

        int count = list.Count;
        return count switch
        {
            0 => default,
            1 => list[0],
            _ => list[rng.Int(count)]
        };
    }
    /// <summary>
    /// Returns a random item within a collection. Every element has the same chance to be returned.
    /// </summary>
    /// <remarks>
    /// This may iterate the collection twice due to first needing to count and then get an element at a random position.
    /// </remarks>
    /// <typeparam name="T"> The type of items in the collection. </typeparam>
    /// <param name="rng"> The source randomness. </param>
    /// <param name="collection"> The collection to pick an item from. </param>
    /// <returns> A random item or <see langword="default"/> if the collection is empty. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="collection"/> is null. </exception>
    public static T? Item<T>(this IRng rng, IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(collection);

        int count = collection.Count();
        return count switch
        {
            0 => default,
            1 => collection.First(),
            _ => collection.ElementAt(rng.Int(count))
        };
    }
}
