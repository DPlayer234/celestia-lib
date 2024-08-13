using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib.Memory;

/// <inheritdoc cref="SpanEx"/>
/// <typeparam name="T"> The element type of the spans. </typeparam>
public static class SpanEx<T>
{
    /// <summary>
    /// Creates a span over the same memory region, specifying the element type as a less derived one than the source.
    /// </summary>
    /// <typeparam name="TDerived"> The source type. This needs to be a base type of <typeparamref name="T"/>. </typeparam>
    /// <param name="span"> The span to cast. </param>
    /// <returns> The same span, with all elements cast up to <typeparamref name="TDerived"/>. </returns>
    public static ReadOnlySpan<T> CastUp<TDerived>(ReadOnlySpan<TDerived> span)
        where TDerived : class?, T
    {
        // This method essentially mirrors the behavior of ImmutableArray<T>.CastUp.
        // The only notable difference is that this method is not and cannot be safely reversible.
        // Also see: https://github.com/dotnet/runtime/issues/96952

        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TDerived, T>(ref MemoryMarshal.GetReference(span)), span.Length);
    }
}

/// <summary>
/// Provides additional methods to work with spans.
/// </summary>
public static class SpanEx
{
    /// <summary>
    /// Creates a span from the start of an array.
    /// </summary>
    /// <remarks>
    /// Unlike the <see cref="Span{T}"/> constructor, this performs no validation that <paramref name="count"/> is 0
    /// if you pass <see langword="null"/> for the <paramref name="array"/>.
    /// </remarks>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="array"> The array to convert. </param>
    /// <param name="count"> The amount of items to include. </param>
    /// <returns> The span over the start of the array. </returns>
    /// <exception cref="ArrayTypeMismatchException"> <typeparamref name="T"/> is a reference type and the type of <paramref name="array"/> doesn't exactly match. </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> CreateFromStart<T>(T[]? array, int count)
    {
        Span<T> span = default;
        if (array != null) span = new(array, 0, count);
        return span;
    }

    /// <summary>
    /// Creates a read-only span from the start of an array.
    /// </summary>
    /// <remarks>
    /// Unlike the <see cref="ReadOnlySpan{T}"/> constructor, this performs no validation that <paramref name="count"/> is 0
    /// if you pass <see langword="null"/> for the <paramref name="array"/>.
    /// </remarks>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="array"> The array to convert. </param>
    /// <param name="count"> The amount of items to include. </param>
    /// <returns> The span over the start of the array. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> CreateReadOnlyFromStart<T>(T[]? array, int count)
    {
        ReadOnlySpan<T> span = default;
        if (array != null) span = new(array, 0, count);
        return span;
    }

    internal static bool TryGetSpan<T>(ICollection<T> collection, out ReadOnlySpan<T> span)
    {
        return TryGetSpan(collection, collection.Count, out span);
    }

    internal static bool TryGetSpan<T>(IReadOnlyCollection<T> collection, out ReadOnlySpan<T> span)
    {
        return TryGetSpan(collection, collection.Count, out span);
    }

    private static bool TryGetSpan<T>(object collection, int count, out ReadOnlySpan<T> span)
    {
        span = default;

        if (count == 0)
            goto Success;

        switch (collection)
        {
            case T[] array:
                span = array;
                goto Success;
            case List<T> list:
                span = CollectionsMarshal.AsSpan(list);
                goto Success;
            case IAsSpan<T> asSpan:
                span = asSpan.AsSpan();
                goto Success;
        }

        return false;

    Success:
        return true;
    }
}
