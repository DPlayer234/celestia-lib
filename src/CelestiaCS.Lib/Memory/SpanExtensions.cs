using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib.Memory;

/// <summary>
/// Provides extension methods to cast types to spans.
/// </summary>
public static class SpanExtensions
{
    /// <summary> Casts the <paramref name="span"/> to a <see cref="ReadOnlySpan{T}"/>. </summary>
    /// <remarks> This method is provided for when the implicit conversion cannot be inferred. </remarks>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="span"> The span to cast. </param>
    /// <returns> The same span. </returns>
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this Span<T> span) => span;

    /// <summary> Casts the <paramref name="array"/> to a <see cref="ReadOnlySpan{T}"/>. </summary>
    /// <remarks> This method is provided for when first casting to <see cref="Span{T}"/> may throw or the implicit conversion cannot be inferred. </remarks>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="array"> The array to cast. </param>
    /// <returns> The array as a span. </returns>
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[]? array) => new ReadOnlySpan<T>(array);

    /// <summary> Creates a <see cref="ReadOnlySpan{T}"/> including all elements of the array starting at <paramref name="start"/>. </summary>
    /// <remarks> This method is provided for when first casting to <see cref="Span{T}"/> may throw. </remarks>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="array"> The array to cast. </param>
    /// <param name="start"> The index of the first item. </param>
    /// <returns> The array slice as a span. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="start"/> is not within range of the <paramref name="array"/>. </exception>
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[]? array, int start)
    {
        if (array == null)
            return ThrowIfNotDefaultOrReturnEmpty<T, int>(start);

        int length = array.Length - start;
        return new ReadOnlySpan<T>(array, start, length);
    }

    /// <summary> Creates a <see cref="ReadOnlySpan{T}"/> including all elements of the array starting at <paramref name="startIndex"/>. </summary>
    /// <remarks> This method is provided for when first casting to <see cref="Span{T}"/> may throw. </remarks>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="array"> The array to cast. </param>
    /// <param name="startIndex"> The index of the first item. </param>
    /// <returns> The array slice as a span. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="startIndex"/> is not within range of the <paramref name="array"/>. </exception>
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[]? array, Index startIndex)
    {
        if (array == null)
            return ThrowIfNotDefaultOrReturnEmpty<T, Index>(startIndex);

        int actualIndex = startIndex.GetOffset(array.Length);
        int length = array.Length - actualIndex;
        return new ReadOnlySpan<T>(array, actualIndex, length);

    }

    /// <summary> Creates a <see cref="ReadOnlySpan{T}"/> including <paramref name="length"/> elements of the array starting at <paramref name="start"/>. </summary>
    /// <remarks> This method is provided for when first casting to <see cref="Span{T}"/> may throw. </remarks>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="array"> The array to cast. </param>
    /// <param name="start"> The index of the first item. </param>
    /// <param name="length"> The length of the span. </param>
    /// <returns> The array slice as a span. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="start"/> and <paramref name="length"/> do not describe a valid range within <paramref name="array"/>. </exception>
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[]? array, int start, int length) => new ReadOnlySpan<T>(array, start, length);

    /// <summary> Creates a <see cref="ReadOnlySpan{T}"/> including the elements defined by the <paramref name="range"/>. </summary>
    /// <remarks> This method is provided for when first casting to <see cref="Span{T}"/> may throw. </remarks>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="array"> The array to cast. </param>
    /// <param name="range"> The range of items to include. </param>
    /// <returns> The array slice as a span. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="range"/> does not describe a valid range within <paramref name="array"/>. </exception>
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[]? array, Range range)
    {
        if (array == null)
            return ThrowIfNotDefaultOrReturnEmpty<T, Range>(range);

        (int start, int length) = range.GetOffsetAndLength(array.Length);
        return new ReadOnlySpan<T>(array, start, length);
    }

    /// <summary> Slices the span at the first occurrence of <see langword="null"/> or <see langword="default"/>. </summary>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="span"> The span to slice. </param>
    /// <returns> A span terminated by the first null-equivalent. </returns>
    public static Span<T> NullTerminate<T>(this Span<T?> span) where T : IEquatable<T?>
    {
        return TerminateWith(span, default)!;
    }

    /// <inheritdoc cref="NullTerminate{T}(Span{T})"/>
    public static ReadOnlySpan<T> NullTerminate<T>(this ReadOnlySpan<T?> span) where T : IEquatable<T?>
    {
        return TerminateWith(span, default)!;
    }

    /// <summary> Slices the span at the first occurrence of <paramref name="value"/>. </summary>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="span"> The span to slice. </param>
    /// <param name="value"> The value to terminate the span at. </param>
    /// <returns> A span terminated by the first occurrence of <paramref name="value"/>. </returns>
    public static Span<T> TerminateWith<T>(this Span<T> span, T value) where T : IEquatable<T>?
    {
        int length = span.IndexOf(value);
        if (length < 0) length = span.Length;
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), length);
    }

    /// <inheritdoc cref="TerminateWith{T}(Span{T}, T)"/>
    public static ReadOnlySpan<T> TerminateWith<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
    {
        int length = span.IndexOf(value);
        if (length < 0) length = span.Length;
        return MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(span), length);
    }

    /// <summary> Writes all elements in <paramref name="first"/>, except those in <paramref name="second"/>, to the <paramref name="destination"/>. </summary>
    /// <remarks> Unlike LINQ's except, this may return duplicates. </remarks>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="first"> The first span. </param>
    /// <param name="second"> The second span. </param>
    /// <param name="destination"> The output destination. </param>
    /// <returns> The amount of elements written to <paramref name="destination"/>. </returns>
    public static int Except<T>(this ReadOnlySpan<T> first, ReadOnlySpan<T> second, Span<T> destination) where T : IEquatable<T>?
    {
        Debug.Assert(destination.Length >= first.Length);

        int len = 0;
        foreach (var value in first)
        {
            if (!second.Contains(value))
                destination[len++] = value;
        }

        return len;
    }

    /// <summary> Writes all elements in <paramref name="first"/>, except those in <paramref name="second"/>, to the <paramref name="destination"/>. </summary>
    /// <remarks> Unlike LINQ's except, this may return duplicates. </remarks>
    /// <typeparam name="T"> The type of the items. </typeparam>
    /// <param name="first"> The first span. </param>
    /// <param name="second"> The second span. </param>
    /// <param name="destination"> The output destination. </param>
    /// <param name="comparer"> An equality comparer to use. </param>
    /// <returns> The amount of elements written to <paramref name="destination"/>. </returns>
    public static int Except<T>(this ReadOnlySpan<T> first, ReadOnlySpan<T> second, Span<T> destination, IEqualityComparer<T> comparer)
    {
        Debug.Assert(destination.Length >= first.Length);

        int len = 0;
        foreach (var value in first)
        {
            if (!Contains(second, value, comparer))
                destination[len++] = value;
        }

        return len;
        
        static bool Contains(ReadOnlySpan<T> span, T value, IEqualityComparer<T> comparer)
        {
            foreach (var item in span)
            {
                if (comparer.Equals(item, value))
                    return true;
            }

            return false;
        }
    }

    private static ReadOnlySpan<T> ThrowIfNotDefaultOrReturnEmpty<T, TArgument>(TArgument argument) where TArgument : IEquatable<TArgument>
    {
        if (!argument.Equals(default))
            ThrowHelper.ArgumentOutOfRange();

        return default;
    }
}
