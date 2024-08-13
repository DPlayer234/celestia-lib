using System;
using System.Collections.Generic;
using CelestiaCS.Lib.Collections;

namespace CelestiaCS.Lib.Random;

/// <summary>
/// Provides extensions for <seealso cref="IRng"/> instances to pick sets of unique collection items.
/// </summary>
public static class RngUniquePickExtensions
{
    /// <summary>
    /// Creates a unique item picker from the given list of items.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="rng"> The source randomness. </param>
    /// <param name="items"> The items to pick from. </param>
    /// <returns> The unique item picker. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="items"/> is null. </exception>
    public static IUniquePicker<T> CreateUniquePicker<T>(this IRng rng, IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(items);

        return new UniquePicker<T>(rng, items);
    }

    /// <summary>
    /// Picks <paramref name="count"/> items from <paramref name="items"/> without duplicates.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="rng"> The source randomness. </param>
    /// <param name="items"> The items to pick from. </param>
    /// <param name="count"> The amount of items to pick. </param>
    /// <returns> The picked items. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="items"/> is null. </exception>
    /// <exception cref="ArgumentException"> <paramref name="count"/> is larger than the element count of <paramref name="items"/>. </exception>
    public static T[] PickUniqueItems<T>(this IRng rng, IReadOnlyList<T> items, int count)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(items);

        if (count == 0)
            return [];
        if (count > items.Count)
            ThrowHelper.Argument(nameof(count), "The amount of items is smaller than the given count");

        var picker = new UniquePicker<T>(rng, items);

        T[] result = new T[count];
        for (int i = 0; i < count; i++)
            result[i] = picker.Next();

        return result;
    }

    private sealed class UniquePicker<T> : IUniquePicker<T>
    {
        private readonly IRng _rng;
        private readonly IReadOnlyList<T> _source;
        private ValueList<int> _indices;

        public UniquePicker(IRng rng, IReadOnlyList<T> items)
        {
            _rng = rng;
            _source = items;
        }

        public int ItemsLeft => _source.Count - _indices.Count;

        public T Next()
        {
            int left = ItemsLeft;
            if (left <= 0)
                ThrowHelper.InvalidOperation("No more items to pick.");

            int index = _rng.Int(0, left);
            var indices = _indices.AsReadOnlySpan();

            int i = 0;
            for (; i < indices.Length; i++)
            {
                if (index < indices[i]) break;
                else index += 1;
            }

            _indices.Insert(i, index);
            return _source[index];
        }

        public override string ToString() => $"UniquePicker<{typeof(T).Name}>[{_indices.Count}/{_source.Count}]";
    }
}
