using System;
using System.Buffers;

namespace CelestiaCS.Lib.Memory;

/// <summary>
/// Defines a <see cref="IMemoryOwner{T}"/> that holds an array.
/// </summary>
/// <remarks>
/// The array may be specified to have been rented from the shared <see cref="ArrayPool{T}"/>.
/// If rented, disposing the instance will return the array to the pool.
/// </remarks>
/// <typeparam name="T"> The type of the elements. </typeparam>
public sealed class ArrayMemoryOwner<T> : IMemoryOwner<T>
{
    private ArraySegment<T> _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayMemoryOwner"/> class.
    /// </summary>
    /// <param name="buffer"> The owned buffer. </param>
    /// <param name="isRented"> Whether the buffer was rented and should be returned later. </param>
    /// <param name="clearOnDisposal"> If rented, whether to clear the array on disposal. </param>
    /// <exception cref="ArgumentException"> <paramref name="buffer"/> wraps a null array. </exception>
    public ArrayMemoryOwner(ArraySegment<T> buffer, bool isRented, bool clearOnDisposal = false)
    {
        if (buffer.Array == null)
            ThrowHelper.Argument(nameof(buffer), "ArraySegment's buffer must not be null.");

        _buffer = buffer;
        IsRented = isRented;
        ClearOnDisposal = clearOnDisposal;
    }

    /// <inheritdoc/>
    public Memory<T> Memory
    {
        get
        {
            var b = _buffer;
            if (b.Array == null)
            {
                ThrowHelper.Disposed(this);
            }

            return b;
        }
    }

    /// <summary>
    /// Gets whether the backing array was rented.
    /// </summary>
    public bool IsRented { get; }

    /// <summary>
    /// Gets or sets whether to clear the array on disposal.
    /// This has no effect if the array isn't rented.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="false"/>.
    /// </remarks>
    public bool ClearOnDisposal { get; set; }

    /// <summary>
    /// If specified as rented, returns the held buffer to the shared <see cref="ArrayPool{T}"/>.
    /// </summary>
    public void Dispose()
    {
        var arr = _buffer.Array;
        if (arr != null)
        {
            _buffer = default;
            if (IsRented)
            {
                ArrayPool<T>.Shared.Return(arr, clearArray: ClearOnDisposal);
            }
        }
    }
}

/// <summary>
/// Provides factory methods to generate <seealso cref="ArrayMemoryOwner{T}"/>.
/// </summary>
public static class ArrayMemoryOwner
{
    /// <summary>
    /// Returns a new, empty instance.
    /// </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    public static ArrayMemoryOwner<T> Empty<T>()
    {
        return new(ArraySegment<T>.Empty, isRented: false);
    }

    /// <summary>
    /// Returns a new instance backed by a rented array with the specified length.
    /// </summary>
    /// <remarks>
    /// Much like using <see cref="ArrayPool{T}.Shared"/> directly, the backing array may be larger.
    /// However, the memory accessed will be sized as specified.
    /// </remarks>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="length"> The length of the memory. </param>
    /// <param name="clearOnDisposal"> Whether to clear the array on disposal. </param>
    public static ArrayMemoryOwner<T> Rent<T>(int length, bool clearOnDisposal = false)
    {
        var array = ArrayPool<T>.Shared.Rent(length);
        return new(new ArraySegment<T>(array, 0, length), isRented: true, clearOnDisposal);
    }

    /// <summary>
    /// Returns a new instance backed by a previously rented array.
    /// </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="buffer"> The buffer to expose. </param>
    /// <param name="clearOnDisposal"> Whether to clear the array on disposal. </param>
    /// <exception cref="ArgumentException"> <paramref name="buffer"/> wraps a null array. </exception>
    public static ArrayMemoryOwner<T> FromRented<T>(ArraySegment<T> buffer, bool clearOnDisposal = false)
    {
        return new(buffer, isRented: true, clearOnDisposal);
    }

    /// <summary>
    /// Returns a new instance backed by a previously rented array.
    /// </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="array"> The backing array. </param>
    /// <param name="length"> How much of the array should be exposed. </param>
    /// <param name="clearOnDisposal"> Whether to clear the array on disposal. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="array"/> is null. </exception>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="length"/> is negative. </exception>
    public static ArrayMemoryOwner<T> FromRented<T>(T[] array, int length, bool clearOnDisposal = false)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        return new(new ArraySegment<T>(array, 0, length), isRented: true, clearOnDisposal);
    }

    /// <summary>
    /// Returns a new instance backed by an array that was not rented.
    /// </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="buffer"> The buffer to expose. </param>
    /// <exception cref="ArgumentException"> <paramref name="buffer"/> wraps a null array. </exception>
    public static ArrayMemoryOwner<T> FromNotRented<T>(ArraySegment<T> buffer)
    {
        return new(buffer, isRented: false);
    }

    /// <summary>
    /// Returns a new instance backed by an array that was not rented.
    /// </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <param name="array"> The backing array. </param>
    /// <param name="length"> How much of the array should be exposed. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="array"/> is null. </exception>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="length"/> is negative. </exception>
    public static ArrayMemoryOwner<T> FromNotRented<T>(T[] array, int length)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        return new(new ArraySegment<T>(array, 0, length), isRented: false);
    }
}
