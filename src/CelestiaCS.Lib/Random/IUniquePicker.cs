using System;
using System.Collections;
using System.Collections.Generic;
using CelestiaCS.Lib.Collections.Internal;

namespace CelestiaCS.Lib.Random;

/// <summary>
/// Defines a type that lets you pick out items without duplicates.
/// </summary>
/// <typeparam name="T"> The type of items. </typeparam>
public interface IUniquePicker<T>
{
    /// <summary>
    /// How many items are left to pick.
    /// </summary>
    int ItemsLeft { get; }

    /// <summary>
    /// Gets the next random item.
    /// </summary>
    /// <returns> The next random item. </returns>
    /// <exception cref="InvalidOperationException"> There are no more items left to return. </exception>
    T Next();
}

/// <summary>
/// Provides extension methods for <see cref="IUniquePicker{T}"/> instances.
/// </summary>
public static class UniquePickerExtensions
{
    /// <summary>
    /// Determines if any items are left to be picked.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="picker"> The picker to use. </param>
    /// <returns> If any items are left. </returns>
    public static bool AnyLeft<T>(this IUniquePicker<T> picker)
    {
        return picker.ItemsLeft != 0;
    }

    /// <summary>
    /// Allows enumerating up to the specified amount of randomly picked items.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="picker"> The picker to use. </param>
    /// <param name="take"> The amount of items to take at most. </param>
    /// <returns> An enumerator to use. </returns>
    public static UniqueTakeEnumerator<T> Take<T>(this IUniquePicker<T> picker, int take)
    {
        return new(picker, take);
    }

    public struct UniqueTakeEnumerator<T> : IStructEnumerator<T>, IEnumerable<T>
    {
        private int _leftToTake;
        private readonly IUniquePicker<T> _picker;

        internal UniqueTakeEnumerator(IUniquePicker<T> picker, int leftToTake)
        {
            ArgumentNullException.ThrowIfNull(picker);

            _picker = picker;
            _leftToTake = leftToTake;
            Current = default!;
        }

        public T Current { get; private set; }

        public bool MoveNext()
        {
            if (_picker.AnyLeft() && _leftToTake > 0)
            {
                _leftToTake -= 1;
                Current = _picker.Next();
                return true;
            }

            return false;
        }

        public readonly UniqueTakeEnumerator<T> GetEnumerator() => this;

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => new StructEnumerator<T, UniqueTakeEnumerator<T>>(this);
        readonly IEnumerator IEnumerable.GetEnumerator() => new StructEnumerator<T, UniqueTakeEnumerator<T>>(this);
    }
}
