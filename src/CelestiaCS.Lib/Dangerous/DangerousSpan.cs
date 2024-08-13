using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib.Dangerous;

/// <summary>
/// Provides dangerous operations for spans. Some of these may AV the process if used incorrectly.
/// </summary>
public static class DangerousSpan
{
    /// <summary>
    /// Gets the reference to a span element without a bounds check.
    /// </summary>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="span"> The span. </param>
    /// <param name="index"> The span index. </param>
    /// <returns> A reference to the element. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceAt<T>(Span<T> span, int index)
    {
        Debug.Assert((uint)index < (uint)span.Length);

        return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)index);
    }

    /// <inheritdoc cref="GetReferenceAt{T}(Span{T}, int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T GetReferenceAt<T>(ReadOnlySpan<T> span, int index)
    {
        Debug.Assert((uint)index < (uint)span.Length);

        return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (nint)(uint)index);
    }

    /// <summary>
    /// Creates a mutable span from a read-only one.
    /// This should only be done if the memory is known to be writable and this won't violate other invariants.
    /// </summary>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="span"> The span. </param>
    /// <returns> The same span but mutable. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsMutable<T>(ReadOnlySpan<T> span)
    {
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), span.Length);
    }

    /// <summary> Creates a span over a buffer with a provided element type. </summary>
    /// <remarks> This is essentially lets you treat any type as an inline array. </remarks>
    /// <typeparam name="TBuffer"> The type of the buffer. </typeparam>
    /// <typeparam name="T"> The element type of the span. </typeparam>
    /// <param name="buffer"> The buffer to wrap in a span. </param>
    /// <returns> A span over the buffer. </returns>
    public static Span<T> CreateFromBuffer<TBuffer, T>(ref TBuffer buffer)
        where TBuffer : struct
    {
        int length = Unsafe.SizeOf<TBuffer>() / Unsafe.SizeOf<T>();
        return MemoryMarshal.CreateSpan(ref Unsafe.As<TBuffer, T>(ref buffer), length);
    }

    /// <inheritdoc cref="CreateFromBuffer{TBuffer, T}(ref TBuffer)"/>
    public static ReadOnlySpan<T> CreateFromReadOnlyBuffer<TBuffer, T>(ref readonly TBuffer buffer)
        where TBuffer : struct
    {
        int length = Unsafe.SizeOf<TBuffer>() / Unsafe.SizeOf<T>();
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TBuffer, T>(ref Unsafe.AsRef(in buffer)), length);
    }

    /// <summary>
    /// Creates a <see cref="DangerousSpan{T}"/> from a reference and a length without safety checks.
    /// </summary>
    /// <remarks>
    /// This method mirrors <see cref="MemoryMarshal.CreateSpan{T}(ref T, int)"/> and does not validate
    /// the lifetime of the returned span as usual construction would.
    /// </remarks>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="reference"> The reference to the 0th element. </param>
    /// <param name="length"> The amount of elements in the span. </param>
    /// <returns> An unchecked, dangerous span. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DangerousSpan<T> CreateDangerousSpan<T>(scoped ref T reference, int length)
    {
        return new(ref Unsafe.AsRef(in reference), length);
    }

    /// <summary>
    /// Casts a span unsafely, as if <see cref="Unsafe.As{TFrom, TTo}(ref TFrom)"/> was applied to every element.
    /// This is restricted to reference types to avoid GC holes and memory corruption.
    /// </summary>
    /// <typeparam name="TFrom"> The source type. </typeparam>
    /// <typeparam name="TTo"> The destination type. </typeparam>
    /// <param name="source"> The span to cast. </param>
    /// <returns> The same span, with the provided type. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<TTo> As<TFrom, TTo>(Span<TFrom> source)
        where TFrom : class?
        where TTo : class?
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(source)), source.Length);
    }

    /// <inheritdoc cref="As{TFrom, TTo}(Span{TFrom})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<TTo> As<TFrom, TTo>(ReadOnlySpan<TFrom> source)
        where TFrom : class?
        where TTo : class?
    {
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(source)), source.Length);
    }

    /// <summary> Casts a span of an arbitrary type to a byte span. The original span may not contain references. </summary>
    /// <typeparam name="T"> The type of the original elements. </typeparam>
    /// <param name="span"> The span to convert. </param>
    /// <returns> A byte span over the same memory. </returns>
    public static Span<byte> AsBytes<T>(Span<T> span)
    {
        Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        return MemoryMarshal.CreateSpan(
            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
            checked(span.Length * Unsafe.SizeOf<T>()));
    }

    /// <inheritdoc cref="AsBytes{T}(Span{T})"/>
    public static ReadOnlySpan<byte> AsBytes<T>(ReadOnlySpan<T> span)
    {
        Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
            checked(span.Length * Unsafe.SizeOf<T>()));
    }

    /// <inheritdoc cref="AsBytes{T}(Span{T})"/>
    public static DangerousSpan<byte> AsBytes<T>(DangerousSpan<T> span)
    {
        Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        return new DangerousSpan<byte>(
            ref Unsafe.As<T, byte>(ref span.Reference),
            checked(span.Length * Unsafe.SizeOf<T>()));
    }
}
