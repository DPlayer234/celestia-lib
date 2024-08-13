using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// A simple FIFO queue that allows O(1) enqueuing and dequeuing of items.
/// </summary>
/// <typeparam name="T"> The type of items held. </typeparam>
public struct ValueQueue<T>
{
    private const int InitialSize = 4;

    private T[]? _array;
    private int _head;
    private int _tail;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueQueue{T}"/> struct with an initial capacity.
    /// </summary>
    /// <param name="capacity"> The initial capacity for the buffer. </param>
    public ValueQueue(int capacity)
    {
        _array = new T[capacity];
        _head = _tail = _count = 0;
    }

    /// <summary>
    /// The count of items currently enqueued.
    /// </summary>
    public readonly int Count => _count;

    /// <summary>
    /// The capacity of the current backing storage.
    /// </summary>
    public readonly int Capacity => _array?.Length ?? 0;

    /// <summary>
    /// Adds an item to the end of the queue.
    /// </summary>
    /// <param name="item"> The item to enqueue. </param>
    public void Enqueue(T item)
    {
        var arr = _array;
        if (arr == null || _count == arr.Length)
        {
            arr = EnsureCapacity(_count + 1);
        }

        int tail = _tail;
        arr[tail] = item;

        _tail = MoveNext(arr, tail);
        _count += 1;
    }

    /// <summary>
    /// Tries to dequeue an item from the beginning of the queue. The return value indicates whether <paramref name="item"/> holds a value.
    /// </summary>
    /// <param name="item"> The item that was dequeued, or <see langword="default"/> if the queue was empty. </param>
    /// <returns> Whether an item was dequeued. </returns>
    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        var arr = _array;
        if (arr == null || _count == 0)
        {
            item = default;
            return false;
        }

        int head = _head;
        item = arr[head];

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            arr[head] = default!;
        }

        _head = MoveNext(arr, head);
        _count -= 1;

        return true;
    }

    /// <summary>
    /// Empties the queue.
    /// </summary>
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() &&
            _array is { } arr && _count != 0)
        {
            int tail = _tail;
            int head = _head;

            // If head is before tail, the used section is contiguous,
            // otherwise it wraps around the end back to the beginning.
            if (head < tail)
            {
                Array.Clear(arr, head, _count);
            }
            else
            {
                Array.Clear(arr, head, arr.Length - head);
                Array.Clear(arr, 0, tail);
            }
        }

        _count = 0;
        _head = 0;
        _tail = 0;
    }

    private static int MoveNext(T[] arr, int index)
    {
        int next = index + 1;
        if (next == arr.Length)
        {
            return 0;
        }

        return next;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private T[] EnsureCapacity(int min)
    {
        // Basically a simplified version of ValueList's method

        var arr = _array;
        if (arr == null)
        {
            int size = Math.Max(InitialSize, min);
            _array = arr = new T[size];
        }
        else if (arr.Length < min)
        {
            // Get new array, copy over contents, replace current, delete old array
            // We need to retain the order to minimize problems in cases with exceptions

            int size = Math.Max(arr.Length * 2, min);
            var newArr = new T[size];

            int tail = _tail;
            int head = _head;

            // See comment in Clear()
            // Also, we always copy the used part to be in [0.._count] after.
            if (head < tail)
            {
                Array.Copy(arr, head, newArr, 0, length: _count);
            }
            else
            {
                int preLen = arr.Length - head;
                Array.Copy(arr, head, newArr, 0, length: preLen);
                Array.Copy(arr, 0, newArr, preLen, length: tail);
            }

            // _tail always must be less than _array.Length.
            // But, since we only enter this branch if the new size is greater than the _array.Length
            // (and as such also greater than _count) we can simply assign the _count to the _tail
            // after the normalizing copy operation above.
            _array = arr = newArr;
            _head = 0;
            _tail = _count;
        }

        return arr;
    }
}
