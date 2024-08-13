using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CelestiaCS.Lib.Collections;

namespace CelestiaCS.Lib.Memory;

/// <summary>
/// An in-memory stream that uses chunks instead of a contiguous memory region.
/// Therefore, resizing can be done without copying.
/// </summary>
/// <remarks>
/// The internal buffer chunks are rented from <see cref="ArrayPool{T}.Shared"/> by default.
/// The allocation algorithm will pick new buffers so, that the capacity doubles each time until the maximum chunk size is reached.
/// </remarks>
public sealed class ChunkedMemoryStream : Stream
{
    // The array pool's maximum pooled size is ~1 GiB.
    // We cap it to 16 MiB as we'll likely never even need that much.
    private const int MaxChunkSizeConst = 16 * 1024 * 1024;

    private int _nextChunkSize;
    private ValueList<byte[]> _chunks;

    private ChunkPosition _position;
    private ChunkPosition _length;

    private readonly bool _clearRentedArrays;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkedMemoryStream"/> class.
    /// </summary>
    /// <param name="minChunkSize"> The minimum size of rented chunks. </param>
    /// <param name="clearRentedArrays"> Whether to clear rented arrays upon disposal. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="minChunkSize"/> is negative or zero. </exception>
    public ChunkedMemoryStream(int minChunkSize, bool clearRentedArrays = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minChunkSize);

        // Allocate backing storage here since IsDisposed
        // checks whether chunks is allocated.
        _chunks = new(ValueList.InitialSize);

        _nextChunkSize = Math.Min(minChunkSize, MaxChunkSizeConst);
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
    public override long Length => GetSize(_length);

    /// <summary>
    /// Gets the current capacity of the stream.
    /// </summary>
    public long Capacity => GetCapacity();

    /// <summary>
    /// Gets or sets the current position within the stream.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"> Attempted to set a negative position. </exception>
    public override long Position
    {
        get => GetSize(_position);
        set => _position = ToSize(value);
    }

    /// <summary>
    /// The maximum size a chunk may have.
    /// </summary>
    /// <remarks>
    /// Larger chunks may be allocated when seeking past the end or setting greater lengths.
    /// </remarks>
    public static int MaxChunkSize => MaxChunkSizeConst;

    private bool IsDisposed => !_chunks.IsAllocated;

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        EnsureNotDisposed();

        int read = 0;
        var chunk = GetCurrentReadChunk().Span;
        while (chunk.Length < buffer.Length)
        {
            if (chunk.IsEmpty)
                goto Exit;

            chunk.CopyTo(buffer);
            read += chunk.Length;

            buffer = buffer[chunk.Length..];
            chunk = GetNextReadChunk().Span;
        }

        if (chunk.Length != 0)
        {
            chunk[..buffer.Length].CopyTo(buffer);
            read += buffer.Length;

            _position.Inner += buffer.Length;
            FixFastPosition();
        }

    Exit:
        return read;
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        EnsureNotDisposed();

        var chunk = GetCurrentReadChunk().Span;
        if (chunk.IsEmpty)
        {
            return -1;
        }

        _position.Inner += 1;
        FixFastPosition();

        return chunk[0];
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        EnsureNotDisposed();

        // Need to always check Length for consistency:
        // Position may have been changed past Length manually,
        // and every other buffer size would move it in this call.
        if (buffer.Length == 0)
            goto Exit;

        var chunk = GetWriteChunk();
        while (chunk.Length <= buffer.Length)
        {
            buffer[..chunk.Length].CopyTo(chunk);
            buffer = buffer[chunk.Length..];

            _position.Outer += 1;
            _position.Inner = 0;

            if (buffer.Length == 0)
                goto Exit;

            chunk = GetWriteChunk();
        }

        if (buffer.Length != 0)
        {
            buffer.CopyTo(chunk);

            _position.Inner += buffer.Length;
            FixFastPosition();
        }

    Exit:
        if (_position.IsAfter(_length)) _length = _position;
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        EnsureNotDisposed();

        var chunk = GetWriteChunk();
        chunk[0] = value;

        _position.Inner += 1;
        FixFastPosition();

        if (_position.IsAfter(_length)) _length = _position;
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
                Position = GetSize(_length) + offset;
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

        var chunk = GetCurrentReadChunk().Span;
        while (!chunk.IsEmpty)
        {
            destination.Write(chunk);
            chunk = GetNextReadChunk().Span;
        }
    }

    /// <inheritdoc/>
    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        ValidateCopyToArguments(destination, bufferSize);
        EnsureNotDisposed();

        cancellationToken.ThrowIfCancellationRequested();

        var chunk = GetCurrentReadChunk().Memory;
        while (!chunk.IsEmpty)
        {
            await destination.WriteAsync(chunk, cancellationToken);
            chunk = GetNextReadChunk().Memory;
        }
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

        int size = Read(buffer, offset, count);
        return Task.FromResult(size);
    }

    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int size = Read(buffer.Span);
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
    /// Sets the current length of the stream. Will not allocate extra space until a subsequent write. Only moves the position if down-sized.
    /// </summary>
    public override void SetLength(long value)
    {
        EnsureNotDisposed();

        _length = ToSize(value);
        if (_position.IsAfter(_length)) _position = _length;
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
        if (IsDisposed) return;

        // `IsDisposed` is tied to the whether the list is allocated.
        // Resetting it marks this stream as disposed.
        var chunks = _chunks;
        _chunks = default;

        foreach (var chunk in chunks)
        {
            ArrayPool<byte>.Shared.Return(chunk, _clearRentedArrays);
        }
    }

    // This specifically fixes the position after a one-time advance
    // by at most the current chunk's length
    private void FixFastPosition()
    {
        int length = _chunks[_position.Outer].Length;
        if (_position.Inner >= length)
        {
            _position.Outer += 1;
            _position.Inner -= length;
        }
    }

    private long GetCapacity()
    {
        EnsureNotDisposed();

        long result = 0;
        foreach (var c in _chunks)
        {
            checked { result += c.Length; }
        }

        return result;
    }

    #region Convert ChunkPosition to scalar

    private long GetSize(ChunkPosition pos)
    {
        EnsureNotDisposed();

        if (pos.Outer == 0)
            return pos.Inner;

        var chunks = _chunks.AsReadOnlySpan();
        Debug.Assert(pos.Outer <= chunks.Length);
        Debug.Assert(chunks.Length >= 1);

        long result = 0;
        foreach (var c in chunks[..pos.Outer])
        {
            checked { result += c.Length; }
        }

        checked { result += pos.Inner; }
        return result;
    }

    private ChunkPosition ToSize(long value)
    {
        EnsureNotDisposed();

        if (value < 0)
            ThrowHelper.ArgumentOutOfRange(nameof(value), value, "Length and Position must be positive.");

        ChunkPosition result = default;

        if (value == 0)
            return result;

        var chunks = _chunks.AsReadOnlySpan();
        for (int i = 0; true; i++)
        {
            uint length;
            if (i >= chunks.Length)
            {
                // As an aside: Arrays of 1-byte sized types are not subject to the limit of
                // Array.MaxLength and instead may allocate the full int.MaxValue range.
                if (value > int.MaxValue)
                    ThrowHelper.InvalidOperation("Cannot seek more than Int32.MaxValue bytes past the end of capacity of a ChunkedMemoryStream.");

                // We pick +1 here since this has to be the final loop iteration.
                // If we end up here, result.Outer == chunks.Length, which is the maximum it may be.
                length = (uint)int.MaxValue + 1;
            }
            else
            {
                length = (uint)chunks[i].Length;
            }

            if (value >= length)
            {
                value -= length;
                result.Outer = i + 1;
            }
            else
            {
                result.Inner = (int)value;
                break;
            }
        }

        return result;
    }

    #endregion

    #region Getting Chunks

    private Chunk GetCurrentReadChunk()
    {
        var pos = _position;
        var length = _length;

        Debug.Assert(pos.Outer <= _chunks.Count);

        if (pos.Outer == _chunks.Count)
            return Chunk.Empty;

        Debug.Assert(pos.Outer < length.Outer || pos.Inner <= length.Inner);

        var chunk = _chunks[pos.Outer];
        if (pos.Outer == length.Outer)
            return new Chunk(chunk, pos.Inner..length.Inner);

        return new Chunk(chunk, pos.Inner..);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Chunk GetNextReadChunk()
    {
        _position.Outer += 1;
        _position.Inner = 0;
        return GetCurrentReadChunk();
    }

    private Span<byte> GetWriteChunk()
    {
        var pos = _position;
        byte[] chunk;

        Debug.Assert(pos.Outer <= _chunks.Count);

        if (pos.Outer == _chunks.Count)
        {
            // Ensure we allocate enough to cover both Length and Position.
            // If Position is greater, also make sure we allocate at least
            // 1 extra byte so we don't return an empty span from here.
            var length = _length;
            int size = length.IsAfter(pos) ? length.Inner : (pos.Inner + 1);
            chunk = ExpandCapacity(size);
        }
        else
        {
            Debug.Assert(pos.Inner < _chunks[pos.Outer].Length);
            chunk = _chunks[pos.Outer];
        }

        return chunk.AsSpan(pos.Inner);
    }

    #endregion

    [MethodImpl(MethodImplOptions.NoInlining)]
    private byte[] ExpandCapacity(int min)
    {
        int chunkSize = Math.Max(min, _nextChunkSize);
        var chunk = ArrayPool<byte>.Shared.Rent(chunkSize);
        _chunks.Add(chunk);

        // `chunk.Length * 2` will overflow for some `min` inputs caused by large seeks
        // beyond the end. We need to ensure we don't get a negative number here so
        // we Max it with the original value.
        _nextChunkSize = Math.Min(Math.Max(chunk.Length * 2, chunkSize), MaxChunkSizeConst);
        return chunk;
    }

    private void EnsureNotDisposed()
    {
        if (IsDisposed)
        {
            ThrowHelper.Disposed(this);
        }
    }

    private struct ChunkPosition
    {
        public int Outer;
        public int Inner;

        public readonly bool IsAfter(ChunkPosition other)
        {
            return Outer > other.Outer
                || (Outer == other.Outer && Inner > other.Inner);
        }
    }

    private readonly struct Chunk
    {
        private readonly byte[]? _array;
        private readonly Range _range;

        public Chunk(byte[] array) : this(array, Range.All) { }
        public Chunk(byte[] array, Range range) => (_array, _range) = (array, range);

        public static Chunk Empty => default;

        public Span<byte> Span => _array.AsSpan(_range);
        public Memory<byte> Memory => _array.AsMemory(_range);
    }
}
