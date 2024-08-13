using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CelestiaCS.Lib.Collections.ArrayAllocators;
using CelestiaCS.Lib.Collections.Internal;
using CelestiaCS.Lib.Dangerous;
using CelestiaCS.Lib.Threading;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// A disposable buffer using pooled storage.
/// </summary>
/// <remarks>
/// This collection is add-only and cannot be modified through the <see cref="ICollection{T}"/> interface.
/// </remarks>
/// <typeparam name="T"> The type of items held. </typeparam>
public sealed class PooledBuffer<T> : IDisposableBuffer<T>, ICollection<T>, ICollection
{
    private const int FirstChunkCapacity = 16;
    private const int NextChunkMult = 2;

    private ValueList<T[], ArrayPoolAllocator> _chunks;
    private int _totalSize;
    private int _lastChunkSize;

    /// <summary>
    /// Initializes a new empty instance of the <see cref="PooledBuffer{T}"/> class.
    /// </summary>
    public PooledBuffer() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledBuffer{T}"/> class, copying all the data from the given enumerable.
    /// </summary>
    /// <param name="source"> The source collection. </param>
    /// <returns> A buffer once the whole collection has been enumerated. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public static PooledBuffer<T> From(IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var buffer = new PooledBuffer<T>();
        return buffer.Initialize(source);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledBuffer{T}"/> class, copying all the data from the given async enumerable.
    /// </summary>
    /// <param name="source"> The source collection. </param>
    /// <returns> A buffer once the whole collection has been enumerated. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public static ValueTask<PooledBuffer<T>> FromAsync(IAsyncEnumerable<T> source, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var buffer = new PooledBuffer<T>();
        return buffer.AddRangeIntlAsync(source, ct);
    }

    /// <summary> Gets the amount of items held. </summary>
    public int Count => _totalSize;

    /// <summary> Gets whether this buffer was disposed. </summary>
    public bool IsDisposed => _lastChunkSize < 0;

    /// <summary>
    /// Disposes and empties this buffer.
    /// </summary>
    public void Dispose()
    {
        // Marks the buffer as disposed
        _lastChunkSize = int.MinValue;

        // Move the chunks into a local and reset the one held in the buffer
        var chunks = _chunks;
        _chunks = default;

        // Then dispose the individual rented sub-buffers and the list itself
        bool clearArray = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        foreach (var c in chunks)
        {
            ArrayPool<T>.Shared.Return(c, clearArray);
        }

        chunks.Dispose();
    }

    /// <summary>
    /// Determines whether this buffer contains any elements.
    /// </summary>
    public bool Any()
    {
        ThrowIfDisposed();
        return _totalSize != 0;
    }

    /// <summary>
    /// Returns the first element in this buffer, or a default value if it is empty.
    /// </summary>
    /// <returns><see langword="default"/> if this buffer is empty; otherwise, the first element.</returns>
    public T? FirstOrDefault()
    {
        ThrowIfDisposed();
        if (_totalSize == 0) return default;
        return _chunks[0][0];
    }

    /// <summary>
    /// Adds an item to end of the buffer.
    /// </summary>
    /// <param name="item"> The item to add. </param>
    public void Add(T item)
    {
        ThrowIfDisposed();

        var (chunk, chunkSize) = GetFreeChunk();

        chunk[chunkSize++] = item;
        _lastChunkSize = chunkSize;
        _totalSize += 1;
    }

    /// <summary>
    /// Adds the elements of a collection to the buffer.
    /// </summary>
    /// <param name="source"> The items to add. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public void AddRange(IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ThrowIfDisposed();

        if (_chunks.Count == 0 && source is ICollection<T> coll)
        {
            InitializeFromICollection(coll);
        }
        else
        {
            AddRangeIntl(source);
        }
    }

    /// <summary>
    /// Adds the elements of an async collection to the buffer.
    /// </summary>
    /// <param name="source"> The items to add. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="source"/> is null. </exception>
    public ValueTask AddRangeAsync(IAsyncEnumerable<T> source, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ThrowIfDisposed();

        return AddRangeIntlAsync(source, ct).AsVoid();
    }

    /// <summary>
    /// Gets an enumerator that allows iterating this collection.
    /// </summary>
    /// <returns> An enumerator. </returns>
    public Enumerator GetEnumerator()
    {
        ThrowIfDisposed();

        return new Enumerator(this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new StructEnumerator<T, Enumerator>(GetEnumerator());
    IEnumerator IEnumerable.GetEnumerator() => new StructEnumerator<T, Enumerator>(GetEnumerator());

    // Methods to add entire enumerables will Dispose of the buffer in case of an exception.
    // The reason for this is two-fold:
    // 1) They are called during init-paths. In that case, the buffer wouldn't be in user hands yet.
    // 2) We end up in a possibly inconsistent state. There is nothing to be done safely with this class after.

    #region Initializers

    // This is basically AddRange but with less checks for initialization
    private PooledBuffer<T> Initialize(IEnumerable<T> source)
    {
        if (source is ICollection<T> coll)
        {
            InitializeFromICollection(coll);
        }
        else
        {
            AddRangeIntl(source);
        }

        return this;
    }

    // Specialized method to handle situations where the buffer is empty and we have an ICollection<T>.
    // Just allocates 1 array of the right size and calls CopyTo.
    private void InitializeFromICollection(ICollection<T> coll)
    {
        Debug.Assert(_chunks.Count == 0 && !IsDisposed);

        int count = coll.Count;
        if (count == 0) return;

        try
        {
            var chunk = ArrayPool<T>.Shared.Rent(count);
            ThrowIfArrayInvalid(chunk);

            _chunks.Add(chunk);

            coll.CopyTo(chunk, 0);

            _lastChunkSize = count;
            _totalSize = count;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    #endregion

    #region AddRange(Async) impl

    // Allocate chunks as needed
    private void AddRangeIntl(IEnumerable<T> source)
    {
        Debug.Assert(!IsDisposed);

        try
        {
            using var enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
                return;

            // If we can cheaply get a size, start with a chunk that
            // can fit the whole thing to hopefully reduce rent count.
            if (!source.TryGetNonEnumeratedCount(out int nextCapacity))
                nextCapacity = FirstChunkCapacity;

            bool itemsLeft = true;
            int totalSize = _totalSize;
            int chunkSize = _lastChunkSize;

            // If there is already something here, fill the current last chunk first.
            if (_chunks.Count > 0)
            {
                var chunk = _chunks[^1];
                nextCapacity -= chunk.Length - chunkSize;

                while (itemsLeft && (uint)chunkSize < (uint)chunk.Length)
                {
                    DangerousArray.WriteAtFast(chunk, chunkSize++, enumerator.Current);
                    itemsLeft = enumerator.MoveNext();
                }

                totalSize += chunkSize - _lastChunkSize;
                nextCapacity = Math.Max(nextCapacity, chunk.Length * NextChunkMult);
            }

            // Add chunks for the remaining items after.
            while (itemsLeft)
            {
                chunkSize = 0;
                var chunk = ArrayPool<T>.Shared.Rent(nextCapacity);
                ThrowIfArrayInvalid(chunk);

                _chunks.Add(chunk);

                while (itemsLeft && (uint)chunkSize < (uint)chunk.Length)
                {
                    DangerousArray.WriteAtFast(chunk, chunkSize++, enumerator.Current);
                    itemsLeft = enumerator.MoveNext();
                }

                totalSize += chunkSize;
                nextCapacity *= NextChunkMult;
            }

            _totalSize = totalSize;
            _lastChunkSize = chunkSize;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<PooledBuffer<T>> AddRangeIntlAsync(IAsyncEnumerable<T> source, CancellationToken ct)
    {
        Debug.Assert(!IsDisposed);

        try
        {
            await using var enumerator = source.GetAsyncEnumerator(ct);

            ct.ThrowIfCancellationRequested();
            if (!await enumerator.MoveNextAsync())
                return this;

            int nextCapacity = FirstChunkCapacity;
            bool itemsLeft = true;
            int totalSize = _totalSize;
            int chunkSize = _lastChunkSize;

            // If there is already something here, fill the current last chunk first.
            if (_chunks.Count > 0)
            {
                var chunk = _chunks[^1];
                nextCapacity -= chunk.Length - chunkSize;

                while (itemsLeft && (uint)chunkSize < (uint)chunk.Length)
                {
                    DangerousArray.WriteAtFast(chunk, chunkSize++, enumerator.Current);

                    ct.ThrowIfCancellationRequested();
                    itemsLeft = await enumerator.MoveNextAsync();
                }

                totalSize += chunkSize - _lastChunkSize;
                nextCapacity = Math.Max(nextCapacity, chunk.Length * NextChunkMult);
            }

            // Add chunks for the remaining items after.
            while (itemsLeft)
            {
                chunkSize = 0;
                var chunk = ArrayPool<T>.Shared.Rent(nextCapacity);
                ThrowIfArrayInvalid(chunk);

                _chunks.Add(chunk);

                while (itemsLeft && (uint)chunkSize < (uint)chunk.Length)
                {
                    DangerousArray.WriteAtFast(chunk, chunkSize++, enumerator.Current);

                    ct.ThrowIfCancellationRequested();
                    itemsLeft = await enumerator.MoveNextAsync();
                }

                totalSize += chunkSize;
                nextCapacity *= NextChunkMult;
            }

            _totalSize = totalSize;
            _lastChunkSize = chunkSize;
        }
        catch
        {
            Dispose();
            throw;
        }

        return this;
    }

    #endregion

    #region Impls to improve LINQ support

    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => this;

    bool ICollection<T>.IsReadOnly => true;

    bool ICollection<T>.Contains(T item)
    {
        ThrowIfDisposed();

        var chunks = _chunks.AsReadOnlySpan();
        for (int i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];
            int chunkSize = i != chunks.Length - 1 ? chunk.Length : _lastChunkSize;

            if (Array.IndexOf(chunk, item, 0, chunkSize) != -1)
                return true;
        }

        return false;
    }

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        ThrowIfDisposed();
        CollectionOfTImplHelper.ValidateCopyToArguments(_totalSize, array, arrayIndex);

        CopyToIntl(array, arrayIndex);
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ThrowIfDisposed();
        CollectionNGImplHelper.ValidateCopyToArguments(_totalSize, array, index);

        CopyToIntl(array, index);
    }

    void ICollection<T>.Add(T item) => throw new NotSupportedException();
    void ICollection<T>.Clear() => throw new NotSupportedException();
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

    private void CopyToIntl(Array array, int index)
    {
        var chunks = _chunks.AsReadOnlySpan();
        for (int i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i];
            int chunkSize = i != chunks.Length - 1 ? chunk.Length : _lastChunkSize;

            Array.Copy(chunk, 0, array, index, chunkSize);
            index += chunkSize;
        }
    }

    #endregion

    internal static PooledBuffer<T>? FromNullIfEmpty(IEnumerable<T> source)
    {
        PooledBuffer<T>? result = null;
        if (source is ICollection<T> coll)
        {
            if (coll.Count != 0)
            {
                result = [];
                result.InitializeFromICollection(coll);
            }
        }
        else
        {
            result = [];
            result.AddRangeIntl(source);
        }

        return result;
    }

    private (T[] chunk, int size) GetFreeChunk()
    {
        Debug.Assert(!IsDisposed);

        T[] chunk;
        int chunkSize;
        if (_chunks.Count == 0)
        {
            Debug.Assert(_lastChunkSize == 0);
            return RentNew(FirstChunkCapacity);
        }
        else
        {
            chunk = _chunks[^1];
            chunkSize = _lastChunkSize;

            if (chunk.Length <= chunkSize)
            {
                return RentNew(chunkSize * NextChunkMult);
            }
        }

        return (chunk, chunkSize);

        (T[] chunk, int size) RentNew(int min)
        {
            chunk = ArrayPool<T>.Shared.Rent(min);
            ThrowIfArrayInvalid(chunk);

            _chunks.Add(chunk);
            _lastChunkSize = 0;

            return (chunk, size: 0);
        }
    }

    private void ThrowIfDisposed()
    {
        if (IsDisposed)
        {
            ThrowHelper.Disposed(this);
        }
    }

    private static void ThrowIfArrayInvalid(T[] array)
    {
        if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
            ThrowHelper.ArrayTypeMismatch();
    }

    public struct Enumerator : IStructEnumerator<T>
    {
        internal readonly PooledBuffer<T>? buffer;
        private T[]? _currentChunk;
        private int _currentChunkSize;

        private int _outerIndex;
        private int _innerIndex;

        internal Enumerator(PooledBuffer<T> pooledBuffer)
        {
            buffer = pooledBuffer;
            _currentChunk = null;
            _currentChunkSize = 0;
            _outerIndex = -1;
            _innerIndex = 0;
        }

        public readonly T Current => _currentChunk![_innerIndex];

        public bool MoveNext()
        {
            // Try to move to the next element in the current chunk
            int innerIndex = _innerIndex + 1;
            if (innerIndex < _currentChunkSize)
            {
                _innerIndex = innerIndex;
                return true;
            }

            return MoveNextRare();
        }

        private bool MoveNextRare()
        {
            var buffer = this.buffer;
            if (buffer == null)
                return false;

            // Try to move to the first element in the next chunk
            var chunks = buffer._chunks.AsReadOnlySpan();
            int outerIndex = _outerIndex + 1;
            if ((uint)outerIndex >= (uint)chunks.Length)
                return false;

            T[] chunk = _currentChunk = chunks[outerIndex];

            _currentChunkSize = outerIndex != chunks.Length - 1 ? chunk.Length : buffer._lastChunkSize;
            _outerIndex = outerIndex;

            _innerIndex = 0;
            return true;
        }
    }
}
