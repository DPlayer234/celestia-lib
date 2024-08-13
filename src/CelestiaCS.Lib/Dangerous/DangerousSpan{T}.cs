using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CelestiaCS.Lib.Dangerous;

/// <summary>
/// A contiguous region of memory, typed as <typeparamref name="T"/>, that, unlike <see cref="Span{T}"/>,
/// does not perform any range or type checks on creation, access, or slicing.
/// </summary>
/// <remarks>
/// Even though checks aren't performed, valid arguments are still expected.
/// F.e. indexing out-of-bounds is not supported and will lead to undefined behavior.
/// The debug build also has asserts for these conditions still.
/// </remarks>
/// <typeparam name="T"> The type of elements. </typeparam>
[SkipLocalsInit]
public readonly ref struct DangerousSpan<T>
{
    private readonly ref T _reference;
    private readonly int _length;

    #region Constructors (with varying degrees of safety)

    /// <summary> Creates a new span over the same region as the input without safety checks. </summary>
    /// <param name="span"> The memory to cover. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DangerousSpan(Span<T> span)
    {
        _reference = ref MemoryMarshal.GetReference(span);
        _length = span.Length;
    }

    /// <summary> Creates a new span over the same region as the input without safety checks. </summary>
    /// <remarks> Writing to the new span should be avoided as the memory might not allow writes or could violate invariants. </remarks>
    /// <param name="span"> The memory to cover. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DangerousSpan(ReadOnlySpan<T> span)
    {
        _reference = ref MemoryMarshal.GetReference(span);
        _length = span.Length;
    }

    /// <summary> Creates a new span over the entire array without safety checks. </summary>
    /// <param name="array"> The target array. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DangerousSpan(T[]? array)
    {
        if (array == null)
        {
            this = default;
            return;
        }

        Debug.Assert(typeof(T).IsValueType || array.GetType() == typeof(T[]));

        _reference = ref MemoryMarshal.GetArrayDataReference(array);
        _length = array.Length;
    }

    /// <summary> Creates a new span over a section of an array without safety checks. </summary>
    /// <remarks> This will not throw, no matter the combination of arguments, and may return invalid spans. </remarks>
    /// <param name="array"> The target array. </param>
    /// <param name="start"> The index at which to begin. </param>
    /// <param name="length"> The amount of elements to cover. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DangerousSpan(T[]? array, int start, int length)
    {
        if (array != null)
        {
            Debug.Assert(typeof(T).IsValueType || array.GetType() == typeof(T[]));
            Debug.Assert((uint)start <= (uint)array.Length);
            Debug.Assert((uint)length <= (uint)(array.Length - start));

            _reference = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start);
        }
        else
        {
            Debug.Assert(start == 0 && length == 0);

            _reference = ref Unsafe.NullRef<T>();
        }

        _length = length;
    }

    /// <summary> Creates a new span over a region of arbitrary memory. </summary>
    /// <param name="ptr"> The 0th element. </param>
    /// <param name="length"> The amount of items to cover. </param>
    /// <exception cref="ArgumentException"> <typeparamref name="T"/> is a reference type or has references. </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe DangerousSpan(void* ptr, int length)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            ThrowHelper.ArgumentCombined("Only types without references are allowed.");

        Debug.Assert(length >= 0);

        _reference = ref Unsafe.AsRef<T>(ptr);
        _length = length;
    }

    /// <summary> Creates a new span over just the single provided item. </summary>
    /// <param name="element"> The single element to cover. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DangerousSpan(ref T element)
    {
        _reference = ref element;
        _length = 1;
    }

    // Impl for DangerousSpan.CreateSpan to mirror MemoryMarshal
    // and because this is even less safe than the ctors above.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal DangerousSpan(ref T reference, int length)
    {
        Debug.Assert(length >= 0);

        _reference = ref reference;
        _length = length;
    }

    #endregion

    /// <summary> Gets an empty instance. </summary>
    public static DangerousSpan<T> Empty => default;

    /// <summary> Gets a reference to the elements at the given <paramref name="index"/>, without bounds checks. </summary>
    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // The forced zero-extension here means that a negative index (except on 32-bit)
            // will actually read an address much further ahead. This is fine since this isn't
            // allowed anyways.
            // The same applies to slicing.

            Debug.Assert((uint)index < (uint)_length);
            return ref Unsafe.Add(ref _reference, (nint)(uint)index);
        }
    }

    /// <summary> The number of items in this span. </summary>
    public int Length => _length;

    /// <summary> Whether this span is empty. </summary>
    public bool IsEmpty => _length == 0;

    internal ref T Reference => ref _reference;

    /// <summary>
    /// Copies the contents of this span into the memory starting with the destination span.
    /// </summary>
    /// <remarks>
    /// This performs no checks on whether the destination has sufficient space and it is simply assumes it does.
    /// </remarks>
    /// <param name="destination"> The destination span. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(DangerousSpan<T> destination)
    {
        var self = AsSpan();
        Debug.Assert(destination.Length >= self.Length);

        // This will eliminate bounds checks within the CopyTo implementation and
        // essentially lets us call directly into Buffer.Memmove.
        var unsafeDestination = MemoryMarshal.CreateSpan(ref destination._reference, self.Length);
        self.CopyTo(unsafeDestination);
    }

    /// <inheritdoc cref="Span{T}.TryCopyTo(Span{T})"/>
    /// <remarks> This will fail as normal if the destination is too small. </remarks>
    public bool TryCopyTo(DangerousSpan<T> destination)
    {
        return AsSpan().TryCopyTo(destination.AsSpan());
    }

    /// <summary>
    /// Creates a new slice, from the given <paramref name="start"/> until the end, without bounds checks.
    /// </summary>
    /// <remarks>
    /// If <paramref name="start"/> is invalid, this will create invalid slices with possibly negative length.
    /// </remarks>
    /// <param name="start"> The index to begin at. </param>
    /// <returns> A new unchecked span slice. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DangerousSpan<T> Slice(int start)
    {
        Debug.Assert((uint)start <= (uint)_length);

        return new(ref Unsafe.Add(ref _reference, (nint)(uint)start), _length - start);
    }

    /// <summary>
    /// Creates a new slice, from the given <paramref name="start"/> for <paramref name="length"/> elements, without bounds checks.
    /// </summary>
    /// <param name="start"> The index to begin at. </param>
    /// <param name="length"> The amount of elements in the slice. </param>
    /// <returns> A new unchecked span slice. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DangerousSpan<T> Slice(int start, int length)
    {
        Debug.Assert((uint)start <= (uint)_length);
        Debug.Assert((uint)length <= (uint)(_length - start));

        return new(ref Unsafe.Add(ref _reference, (nint)(uint)start), length);
    }

    /// <summary>
    /// Creates a mutable, checked span over the same memory area.
    /// </summary>
    /// <returns> An equivalent mutable, checked span. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _reference, _length);

    /// <summary>
    /// Creates a read-only, checked span over the same memory area.
    /// </summary>
    /// <returns> An equivalent read-only, checked span. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref _reference, _length);

    /// <summary>
    /// Returns a string representation of this span.
    /// </summary>
    public override string ToString()
    {
        if (typeof(T) == typeof(char))
        {
            return new string(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, char>(ref _reference), _length));
        }

        return $"DangerousSpan<{typeof(T).Name}>[{_length}]";
    }

    #region Match span API surface

    /// <inheritdoc cref="Span{T}.GetPinnableReference"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ref T GetPinnableReference()
    {
        ref T ret = ref Unsafe.NullRef<T>();
        if (_length != 0) ret = ref _reference;
        return ref ret;
    }

    /// <inheritdoc cref="Span{T}.Clear"/>
    public void Clear() => AsSpan().Clear();

    /// <inheritdoc cref="Span{T}.Fill(T)"/>
    public void Fill(T value) => AsSpan().Fill(value);

    #endregion
}
