using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CelestiaCS.Lib.Collections;
using CelestiaCS.Lib.Collections.ArrayAllocators;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.Random;

/// <summary>
/// Provides extensions for <seealso cref="IRng"/> instances to pick weighted elements.
/// </summary>
public static class RngWeightedPickExtensions
{
    private const int BinarySearchCutoff = 16;

    /// <summary>
    /// Creates a weighted item picker from the given set of items and weights.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="rng"> The source randomness. </param>
    /// <param name="pairs"> The item-weight pairs. </param>
    /// <returns> A weighted item picker. </returns>
    /// <exception cref="ArgumentException"> <paramref name="pairs"/> is empty, or a weight is negative, or the total weight is zero. </exception>
    public static IWeightedPicker<T> CreateWeightedPicker<T>(this IRng rng, ReadOnlySpan<(T item, double weight)> pairs)
    {
        ArgumentNullException.ThrowIfNull(rng);

        var data = CreateWeightedPickerData(rng, pairs);
        return CreateWeightedPicker(data);
    }

    /// <summary>
    /// Creates a weighted item picker from the given set of items and weights.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="rng"> The source randomness. </param>
    /// <param name="pairs"> The item-weight pairs. </param>
    /// <returns> A weighted item picker. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="pairs"/> is null. </exception>
    /// <exception cref="ArgumentException"> <paramref name="pairs"/> is empty, or a weight is negative, or the total weight is zero. </exception>
    public static IWeightedPicker<T> CreateWeightedPicker<T>(this IRng rng, IEnumerable<(T item, double weight)> pairs)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(pairs);

        var data = CreateWeightedPickerData(rng, pairs);
        return CreateWeightedPicker(data);
    }

    /// <summary>
    /// Picks a single item based on the item-weight pairs.
    /// </summary>
    /// <remarks>
    /// If only few items need to be picked, this may be preferable over creating a <see cref="IWeightedPicker{T}"/>.
    /// However, creating one is preferable if the pairs would be reused often.
    /// </remarks>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="rng"> The source randomness. </param>
    /// <param name="pairs"> The item-weight pairs. </param>
    /// <returns> A random weighted item. </returns>
    /// <exception cref="ArgumentException"> <paramref name="pairs"/> is empty, or a weight is negative, or the total weight is zero. </exception>
    public static T PickWeightedItem<T>(this IRng rng, ReadOnlySpan<(T item, double weight)> pairs)
    {
        ArgumentNullException.ThrowIfNull(rng);

        if (pairs.Length == 0)
            ThrowHelper.Argument(nameof(pairs), "The pairs may not be empty.");

        double totalWeight = 0.0;
        foreach (var (_, weight) in pairs)
        {
            if (weight < 0.0)
                ThrowHelper.Argument(nameof(pairs), "Pair weights must be positive.");

            totalWeight += weight;
        }

        if (totalWeight == 0.0)
            ThrowHelper.Argument(nameof(pairs), "At least one weight needs to be non-zero.");

        return PickWeightedItemCore(rng, pairs, totalWeight);
    }

    /// <summary>
    /// Picks a single item based on the item-weight pairs. The enumerable is copied to a temporary buffer.
    /// </summary>
    /// <remarks>
    /// If only few items need to be picked, this may be preferable over creating a <see cref="IWeightedPicker{T}"/>.
    /// However, creating one is preferable if the pairs would be reused often.
    /// </remarks>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="rng"> The source randomness. </param>
    /// <param name="pairs"> The item-weight pairs. </param>
    /// <returns> A random weighted item. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="pairs"/> is null. </exception>
    /// <exception cref="ArgumentException"> <paramref name="pairs"/> is empty, or a weight is negative, or the total weight is zero. </exception>
    public static T PickWeightedItem<T>(this IRng rng, IEnumerable<(T item, double weight)> pairs)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(pairs);

        using ValueList<(T item, double weight), ArrayPoolAllocator> rPairs = new(16);

        double totalWeight = 0.0;
        foreach (var (item, weight) in pairs)
        {
            if (weight < 0.0)
                ThrowHelper.Argument(nameof(pairs), "Pair weights must be positive.");

            totalWeight += weight;
            rPairs.Add((item, weight));
        }

        if (rPairs.Count == 0)
            ThrowHelper.Argument(nameof(pairs), "The pairs may not be empty.");

        if (totalWeight == 0.0)
            ThrowHelper.Argument(nameof(pairs), "At least one weight needs to be non-zero.");

        return PickWeightedItemCore(rng, rPairs.AsReadOnlySpan(), totalWeight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T PickWeightedItemCore<T>(IRng rng, ReadOnlySpan<(T item, double weight)> pairs, double totalWeight)
    {
        double pos = rng.Float(0.0, totalWeight);
        foreach (var (item, weight) in pairs)
        {
            if (weight > pos)
                return item;

            pos -= weight;
        }

        // If we get here, there might be some funky stuff going on with floating point accuracy
        // So just assume we hit the last item.
        return pairs[^1].item;
    }

    private static PickerData<T> CreateWeightedPickerData<T>(IRng rng, ReadOnlySpan<(T item, double weight)> pairs)
    {
        if (pairs.Length == 0)
            ThrowHelper.Argument(nameof(pairs), "The pairs may not be empty.");

        double totalWeight = 0.0;
        var rPairs = new PickerPair<T>[pairs.Length];

        for (int i = 0; i < pairs.Length; i++)
        {
            var (item, weight) = pairs[i];
            if (weight < 0.0)
                ThrowHelper.Argument(nameof(pairs), "Pair weights must be positive.");

            totalWeight += weight;
            rPairs[i] = new(item, totalWeight);
        }

        if (totalWeight == 0.0)
            ThrowHelper.Argument(nameof(pairs), "At least one weight needs to be non-zero.");

        return new(rng, totalWeight, rPairs);
    }

    private static PickerData<T> CreateWeightedPickerData<T>(IRng rng, IEnumerable<(T item, double weight)> pairs)
    {
        double totalWeight = 0.0;

        ValueList<PickerPair<T>> rPairs = new(16);

        foreach (var (item, weight) in pairs)
        {
            if (weight < 0.0)
                ThrowHelper.Argument(nameof(pairs), "Pair weights must be positive.");

            totalWeight += weight;
            rPairs.Add(new(item, totalWeight));
        }

        if (rPairs.Count == 0)
            ThrowHelper.Argument(nameof(pairs), "The pairs may not be empty.");

        if (totalWeight == 0.0)
            ThrowHelper.Argument(nameof(pairs), "At least one weight needs to be non-zero.");

        return new(rng, totalWeight, rPairs.DrainToArray());
    }

    private static IWeightedPicker<T> CreateWeightedPicker<T>(PickerData<T> data)
    {
        return data.Pairs.Length >= BinarySearchCutoff
            ? new BinaryPicker<T>(data)
            : new LinearPicker<T>(data);
    }

    private readonly struct PickerPair<T>(T item, double weight)
    {
        public T Item { get; } = item;
        public double Weight { get; } = weight;
    }

    private readonly struct PickerData<T>(IRng rng, double totalWeight, PickerPair<T>[] pairs)
    {
        public IRng Rng { get; } = rng;
        public double TotalWeight { get; } = totalWeight;
        public PickerPair<T>[] Pairs { get; } = pairs;
    }

    private abstract class PickerBase<T>(PickerData<T> data) : IWeightedPicker<T>
    {
        private readonly PickerData<T> _data = data;

        public abstract T Next();

        protected ReadOnlySpan<PickerPair<T>> GetPairs() => _data.Pairs.AsReadOnlySpan();
        protected double GetPos() => _data.Rng.Float(0.0, _data.TotalWeight);

        public override string ToString() => $"WeightedPicker<{typeof(T).Name}>[{_data.Pairs.Length}]";
    }

    private sealed class LinearPicker<T>(PickerData<T> data) : PickerBase<T>(data)
    {
        public override T Next()
        {
            var pairs = GetPairs();
            double pos = GetPos();

            foreach (ref readonly var pair in pairs)
            {
                if (pair.Weight > pos)
                    return pair.Item;
            }

            // See comment in PickWeightedItemCore
            return pairs[^1].Item;
        }
    }

    private sealed class BinaryPicker<T>(PickerData<T> data) : PickerBase<T>(data)
    {
        public override T Next()
        {
            var pairs = GetPairs();
            double pos = GetPos();

            // Either returns the matching index or
            // the bitwise complement of the next larger element's index.
            int index = pairs.BinarySearch(new Pos(pos));

            // To retain the > logic, if we find an exact match, use the next one.
            if (index >= 0) index += 1;
            else index = ~index;

            if ((uint)index < (uint)pairs.Length)
                return pairs[index].Item;

            // See comment in PickWeightedItemCore
            return pairs[^1].Item;
        }

        private readonly struct Pos(double pos) : IComparable<PickerPair<T>>
        {
            public int CompareTo(PickerPair<T> other) => pos.CompareTo(other.Weight);
        }
    }
}
