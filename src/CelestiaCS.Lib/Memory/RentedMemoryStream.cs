using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CelestiaCS.Lib.Memory;

/// <summary>
/// An in-memory stream that rents its arrays from the shared <see cref="ArrayPool{T}"/>.
/// The backing memory can be exported.
/// </summary>
/// <remarks>
/// Specify the required minimum capacity as such, that resizing won't be needed in most cases.
/// If this isn't possible and lots of data is expected (>64 KB), it is preferable to use <seealso cref="ChunkedMemoryStream"/>.
/// </remarks>
public sealed class RentedMemoryStream : Stream
{
    private byte[]? _array;

    private int _position;
    private int _length;

    private readonly bool _clearRentedArrays;

    /// <summary>
    /// Initializes a new instance of the <see cref="RentedMemoryStream"/> class that has at least the specified initial capacity.
    /// </summary>
    /// <param name="minimumCapacity"> The minimum capacity to initially allocate. </param>
    /// <param name="clearRentedArrays"> Whether to clear rented arrays upon return. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="minimumCapacity"/> is negative or zero. </exception>
    public RentedMemoryStream(int minimumCapacity, bool clearRentedArrays = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumCapacity);

        _array = ArrayPool<byte>.Shared.Rent(minimumCapacity);
        _clearRentedArrays = clearRentedArrays;
    }

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public override bool CanRead => !IsDisposed;

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public override bool CanSeek => !IsDisposed;

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public override bool CanWrite => !IsDisposed;

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public override bool CanTimeout => false;

    /// <summary>
    /// Gets the length of the stream in bytes.
    /// </summary>
    public override long Length => _length;

    /// <summary>
    /// Gets the current capacity of the stream before it has to resize its internal buffer.
    /// </summary>
    public int Capacity
    {
        get
        {
            EnsureNotDisposed();
            return _array.Length;
        }
    }

    /// <summary>
    /// Gets or sets the current position within the stream.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"> Attempted to set a value less than zero or greater than <see cref="int.MaxValue"/>. </exception>
    public override long Position
    {
        get => _position;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, int.MaxValue);
            _position = (int)value;
        }
    }

    [MemberNotNullWhen(false, nameof(_array))]
    private bool IsDisposed => _array == null;

    /// <summary>
    /// Transfers the ownership of the held memory and closes this stream.
    /// </summary>
    /// <returns> The memory now owned by the caller. </returns>
    public IMemoryOwner<byte> TransferAndClose()
    {
        EnsureNotDisposed();

        var b = _array;
        _array = null;

        return ArrayMemoryOwner.FromRented(b, _length, _clearRentedArrays);
    }

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        EnsureNotDisposed();

        int position = _position;
        int length = _length;

        int size = Math.Min(length - position, buffer.Length);
        if (size <= 0) return 0;

        _array.AsSpan(position, size).CopyTo(buffer);
        _position = position + size;
        return size;
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        EnsureNotDisposed();

        int position = _position;
        if (_length <= position)
        {
            return -1;
        }

        int result = _array[position];
        _position = position + 1;
        return result;
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        EnsureNotDisposed();

        int position = _position;
        int endPos = checked(position + buffer.Length);

        EnsureCapacity(endPos);
        buffer.CopyTo(_array.AsSpan(position));

        _position = endPos;
        if (endPos > _length) _length = endPos;
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        EnsureNotDisposed();

        int position = _position;
        int endPos = checked(position + 1);

        EnsureCapacity(endPos);
        _array[position] = value;

        _position = endPos;
        if (endPos > _length) _length = endPos;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        EnsureNotDisposed();

        switch (origin)
        {
            case SeekOrigin.Begin:
                Position = offset;
                break;
            case SeekOrigin.Current:
                Position += offset;
                break;
            case SeekOrigin.End:
                Position = _length + offset;
                break;
            default:
                ThrowHelper.Argument(nameof(origin));
                break;
        }

        return Position;
    }

    /// <inheritdoc/>
    public override void CopyTo(Stream destination, int bufferSize)
    {
        ValidateCopyToArguments(destination, bufferSize);
        EnsureNotDisposed();

        var copyBuff = _array.AsSpan(_position, _length - _position);
        _position = _length;

        destination.Write(copyBuff);
    }

    /// <inheritdoc/>
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        ValidateCopyToArguments(destination, bufferSize);
        EnsureNotDisposed();

        var copyBuff = _array.AsMemory(_position, _length - _position);
        _position = _length;

        return destination.WriteAsync(copyBuff, cancellationToken).AsTask();
    }

    #region Read Overload/Variants

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        return Read(new Span<byte>(buffer, offset, count));
    }

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var size = Read(buffer, offset, count);
        return Task.FromResult(size);
    }

    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var size = Read(buffer.Span);
        return ValueTask.FromResult(size);
    }

    #endregion

    #region Write Overload/Variants

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        Write(new ReadOnlySpan<byte>(buffer, offset, count));
    }

    /// <inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Write(buffer, offset, count);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    #endregion

    /// <summary>
    /// Sets the current length of the stream. May cause reallocation but will only move the position if down-sized.
    /// </summary>
    public override void SetLength(long value)
    {
        EnsureNotDisposed();

        if (value > _length)
        {
            int newLength = checked((int)value);
            EnsureCapacity(newLength);
            _length = newLength;
        }
        else
        {
            _length = (int)value;
            if (_position > _length) _position = _length;
        }
    }

    /// <summary>
    /// Performs no operation. Data does not need to be flushed.
    /// </summary>
    public override void Flush()
    {
        // Nothing needs to be flushed
        EnsureNotDisposed();
    }

    /// <summary>
    /// Performs no operation. Data does not need to be flushed.
    /// </summary>
    /// <returns> A completed task. </returns>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Flush();
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        var b = _array;
        if (b != null)
        {
            _array = null;
            ArrayPool<byte>.Shared.Return(b);
        }
    }

    private void EnsureCapacity(int minimumCapacity)
    {
        Debug.Assert(!IsDisposed, "Caller needs to ensure not disposed.");

        if (_array.Length < minimumCapacity)
        {
            Resize(minimumCapacity);
        }
    }

    private void Resize(int newSize)
    {
        Debug.Assert(!IsDisposed, "Caller needs to ensure not disposed.");

        var temp = ArrayPool<byte>.Shared.Rent(newSize);
        var old = _array;

        old.CopyTo(temp.AsSpan());

        _array = temp;
        ArrayPool<byte>.Shared.Return(old, _clearRentedArrays);
    }

    [MemberNotNull(nameof(_array))]
    private void EnsureNotDisposed()
    {
        if (IsDisposed)
        {
            ThrowHelper.Disposed(this);
        }
    }
}
