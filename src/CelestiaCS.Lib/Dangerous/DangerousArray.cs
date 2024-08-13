using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib.Dangerous;

/// <summary>
/// Provides dangerous operations for arrays. Some of these may AV the process if used incorrectly.
/// </summary>
public static class DangerousArray
{
    /// <summary>
    /// Gets the reference to an array element without a bounds or variance check.
    /// </summary>
    /// <typeparam name="T"> The type of elements. </typeparam>
    /// <param name="array"> The array. </param>
    /// <param name="index"> The array index. </param>
    /// <returns> A reference to the element. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetReferenceAt<T>(T[] array, int index)
    {
        Debug.Assert(array != null);
        Debug.Assert((uint)index < (uint)array.Length);
        Debug.Assert(typeof(T).IsValueType || array.GetType() == typeof(T[]));

        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)index);
    }

    /// <inheritdoc cref="GetReferenceAt{T}(T[], int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly T GetReferenceAt<T>(ImmutableArray<T> array, int index)
    {
        return ref GetReadOnlyReferenceAt(ImmutableCollectionsMarshal.AsArray(array)!, index);
    }

    /// <summary>
    /// Creates a span without any bounds or type checks over a portion of an array.
    /// </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="array"> The array to slice. </param>
    /// <param name="start"> The start index. </param>
    /// <param name="length"> The length of the slice. </param>
    /// <returns> A span that may go over a section of the array. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpan<T>(T[] array, int start, int length)
    {
        Debug.Assert(array != null);
        Debug.Assert((uint)start <= (uint)array.Length);
        Debug.Assert((uint)length <= (uint)(array.Length - start));
        Debug.Assert(typeof(T).IsValueType || array.GetType() == typeof(T[]));

        ref T r = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start);
        return MemoryMarshal.CreateSpan(ref r, length);
    }

    /// <inheritdoc cref="AsSpan"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(T[] array, int start, int length)
    {
        Debug.Assert(array != null);
        Debug.Assert((uint)start <= (uint)array.Length);
        Debug.Assert((uint)length <= (uint)(array.Length - start));

        ref T r = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start);
        return MemoryMarshal.CreateReadOnlySpan(ref r, length);
    }

    /// <inheritdoc cref="AsSpan"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(ImmutableArray<T> array, int start, int length)
    {
        return AsReadOnlySpan(ImmutableCollectionsMarshal.AsArray(array)!, start, length);
    }

    // TL;DR: Fastest way to write/get a ref after doing a manual bounds check
    // Optimally also requires validating the array type but that's "optional"
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteAtFast<T>(T[] array, int index, T item)
    {
        // I literally benchmarked this and looked at the assembly code.
        // Normal array access is faster/same for structs, but slower/same for classes.
        // This should be independent on host, since the difference is which/whether COR helpers are called.

        if (typeof(T).IsValueType)
            array[index] = item;
        else
            GetReferenceAt(array, index) = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T GetReferenceAtFast<T>(T[] array, int index)
    {
        // Same as WriteAtFast
        // When getting a `ref readonly`, just always use `ref array[index]`.

        if (typeof(T).IsValueType)
            return ref array[index];
        else
            return ref GetReferenceAt(array, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T GetReadOnlyReferenceAt<T>(T[] array, int index)
    {
        Debug.Assert(array != null);
        Debug.Assert((uint)index < (uint)array.Length);

        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)index);
    }
}
