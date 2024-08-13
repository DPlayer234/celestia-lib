using System;
using System.Buffers;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib.Memory;

/// <summary>
/// Holds a <see cref="Span{T}"/> that may refer to rented memory.
/// </summary>
/// <typeparam name="T"> The type of items. </typeparam>
public ref struct SpanRenter<T>
{
    private Span<T> _span;
    private T[]? _array;

    /// <summary>
    /// Rents an array with the specified size.
    /// </summary>
    /// <param name="size"> The size to rent. </param>
    /// <returns> A span renter with the right size. </returns>
    public static SpanRenter<T> Rent(int size)
    {
        SpanRenter<T> result;

        var arr = ArrayPool<T>.Shared.Rent(size);
        result._array = arr;
        result._span = arr.AsSpan(0, size);

        return result;
    }

    /// <summary>
    /// Uses the <paramref name="stack"/> space or rents an array if the <paramref name="size"/> is too large.
    /// </summary>
    /// <param name="size"> The required size. </param>
    /// <param name="stack"> The reserved stack space. </param>
    /// <returns> A span renter with the correct size. </returns>
    public static SpanRenter<T> UseStackOrRent(int size, Span<T> stack)
    {
        if (stack.Length >= size)
        {
            SpanRenter<T> result;

            result._array = null;
            result._span = stack[..size];

            return result;
        }

        return Rent(size);
    }

    /// <summary>
    /// Gets the held span. Do not keep the returned value around after returning/disposing.
    /// </summary>
    public readonly Span<T> Span => _span;

    /// <summary>
    /// Gets the whole rented array, if there is one.
    /// </summary>
    public readonly T[]? GetRentedArray() => _array;

    /// <summary> Slices the stored span. </summary>
    /// <param name="range"> The range to slice it to. </param>
    public void SliceSpan(Range range)
    {
        _span = _span[range];
    }

    /// <summary>
    /// Returns the rented buffer. This instance will be empty afterwards.
    /// </summary>
    public void Return()
    {
        var arr = _array;
        this = default;

        if (arr != null)
        {
            bool clearArray = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
            ArrayPool<T>.Shared.Return(arr, clearArray);
        }
    }

    /// <summary>
    /// Returns the rented buffer. This instance will be empty afterwards.
    /// </summary>
    /// <remarks>
    /// For clearer semantics, use <see cref="Return"/> if called explicitly or use a <see langword="using"/> statement.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Dispose() => Return();
}
