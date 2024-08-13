using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CelestiaCS.Lib.Collections.ArrayAllocators;
using CelestiaCS.Lib.Collections.Internal;
using CelestiaCS.Lib.Dangerous;
using CelestiaCS.Lib.Memory;

namespace CelestiaCS.Lib.Collections;

/* When adding a method, consider adding it to ValueList{T} as well!! */

/// <summary>
/// A value-type list to hold items. This struct should not be used after being copied.
/// </summary>
/// <remarks>
/// This type is much like <see cref="ValueList{T}"/> however defines an allocator to use, rather than always creating new backing storage itself.
/// Also, it should be disposed when it is no longer used.
/// </remarks>
/// <typeparam name="T"> The type of items to hold. </typeparam>
/// <typeparam name="TAlloc"> The array allocator to use. </typeparam>
[DebuggerDisplay("Count = {Count}")]
public struct ValueList<T, TAlloc> : IList<T>, IReadOnlyList<T>, IAsSpan<T>, IDisposableBuffer<T>, IList, IValueList
    where TAlloc : IArrayAllocator
{
    // IMPORTANT:
    // The _array is assumed to always be *exactly* T[] (or T : struct).
    // This struct (and its friends) may use unsafe code to access the array
    // in a way that may violate type safety if this doesn't hold and will
    // validate arrays returned by TAlloc to be the correct type.
    // E.g. a string[] might be valid to assign to object[], but this struct
    // does not allow that. This is effectively the same rules Span<T> uses.
    //
    // The `ThrowHelper.NullRefIfNull` calls sprinkled throughout some methods
    // are cheap runtime/JIT asserts that the array isn't null to avoid additional
    // branches for span creation from it.
    // This is done for the return of `EnsureCapacity` and when loading `_array`
    // after checking for a non-zero count.

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [SuppressMessage("Style", "IDE1006:Naming Rules", Justification = "Functionally private, but needed for ValueList<T>")]
    internal T[]? _array;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueList{T, TAlloc}"/> struct with the specified capacity.
    /// </summary>
    /// <remarks>
    /// The actual capacity may be larger, as determined by <typeparamref name="TAlloc"/>.
    /// </remarks>
    /// <param name="capacity"> The capacity to initialize it to. </param>
    /// <exception cref="ArgumentOutOfRangeException"> The capacity is negative. </exception>
    public ValueList(int capacity)
    {
        if (capacity < 0)
            ThrowHelper.ArgumentOutOfRange_CapacityMustBePositive();

        _array = AllocateArrayChecked(capacity);
        _count = 0;
    }

    // Constructor called by ValueList<T> constructors
    internal ValueList(T[] array, int count)
    {
        Debug.Assert(typeof(TAlloc) == typeof(NewArrayAllocator));
        ValueList.ValidateArray(array);

        _array = array;
        _count = count;
    }

    /// <summary>
    /// Gets a reference to an item in the list. The reference is only valid until an item is added or removed.
    /// </summary>
    /// <param name="index"> The index to get an item from. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="index"/> is not in range. </exception>
    public readonly ref T this[int index]
    {
        get
        {
            // Careful!
            // We need to check against the array length as well because
            // the struct might have torn while accessing the indexer.
            // We use DangerousArray to skip the variance check.
            // Both fields may only be loaded once.

            var arr = _array;
            if ((uint)index >= (uint)_count || (uint)index >= (uint)arr!.Length)
                ThrowHelper.ArgumentOutOfRange_IndexMustBeLess();

            return ref DangerousArray.GetReferenceAtFast(arr, index);
        }
    }

    /// <summary>
    /// The count of items currently in the list.
    /// </summary>
    public readonly int Count => _count;

    /// <summary>
    /// The capacity of the current backing storage.
    /// </summary>
    public readonly int Capacity => _array?.Length ?? 0;

    /// <summary>
    /// Whether any backing storage is allocated.
    /// </summary>
    public readonly bool IsAllocated => _array != null;

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Debugger helper.")]
    private readonly Span<T> Content => AsSpan();

    #region Add

    /// <summary>
    /// Adds an item to the list. Expands the capacity if needed.
    /// </summary>
    /// <param name="item"> The item to add. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        var arr = _array;
        int count = _count;
        if (arr != null && (uint)count < (uint)arr.Length)
        {
            DangerousArray.WriteAtFast(arr, count, item);
            _count = count + 1;
        }
        else
        {
            AddWithResize(item);
        }
    }

    // Keep this method here as it's tied to Add(T)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddWithResize(T item)
    {
        int count = _count;
        int newCount = count + 1;
        var arr = ExpandCapacity(newCount);
        arr[count] = item;
        _count = newCount;
    }

    /// <summary>
    /// Adds several items to the list. Expands the capacity if needed.
    /// </summary>
    /// <param name="items"> The items to add. </param>
    public void AddRange(ReadOnlySpan<T> items)
    {
        int count = _count;
        int newCount = count + items.Length;

        var arr = EnsureCapacityUnchecked(newCount);
        ThrowHelper.NullRefIfNull(arr);

        items.CopyTo(arr.AsSpan(count));
        _count = newCount;
    }

    /// <inheritdoc cref="AddRange(ReadOnlySpan{T})"/>
    /// <typeparam name="TDerived"> A derived type. </typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange<TDerived>(ReadOnlySpan<TDerived> items) where TDerived : class?, T
    {
        // This overload exists purely so you can treat spans as covariant for the purposes of AddRange.
        // As a nice side-effect, it also means that `TDerived[].AsReadOnlySpan()` will return a compatible value.
        // (Passing a Span<T> works, but Span<TDerived> has always needed an explicit cast.)
        //
        // Unlike some ImmutableArray related methods, this only supports T and TDerived as reference types.
        // This leads to better symmetry with the types allowed by the IEnumerable<T> overload.

        AddRange(SpanEx<T>.CastUp(items));
    }

    // We could have overloads for T[], TEnumerable : struct, IEnumerable<T> etc
    // but this will likely not be common enough to cause trouble *or* you can
    // just use the ReadOnlySpan<T> overload above.

    /// <inheritdoc cref="AddRange(ReadOnlySpan{T})"/>
    /// <exception cref="ArgumentNullException"> <paramref name="items"/> is <see langword="null"/>. </exception>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null)
            ThrowHelper.ArgumentNull_Items();

        if (items is ICollection<T> coll)
        {
            int itemsCount = coll.Count;
            if (itemsCount == 0) return;

            // Don't modify '_count' until the end in case there is an exception
            // or 'this' and 'items' refer to the same boxed list.
            int count = _count;
            int newCount = count + itemsCount;
            var arr = EnsureCapacityUnchecked(newCount);
            coll.CopyTo(arr, count);
            _count = newCount;
        }
        else if (items.TryGetNonEnumeratedCount(out int itemsCount))
        {
            if (itemsCount == 0) return;

            var arr = EnsureCapacityUnchecked(_count + itemsCount);
            ThrowHelper.NullRefIfNull(arr);

            var span = arr.AsSpan();
            foreach (var item in items)
            {
                span[_count++] = item;
            }
        }
        else
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
    }

    #endregion

    #region Remove

    /// <summary>
    /// Removes an item from the list. Only removes one instance if it is present more than once.
    /// </summary>
    /// <param name="item"> The item to remove. </param>
    /// <returns> If it was found and removed. </returns>
    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index != -1)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the item at a specified index.
    /// </summary>
    /// <param name="index"> The index to remove an item from. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="index"/> is not in range. </exception>
    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)_count)
            ThrowHelper.ArgumentOutOfRange_IndexMustBeLess();

        var arr = _array;
        Debug.Assert(arr != null);

        _count -= 1;
        if (index < _count)
        {
            // We can skip moving the last elements if we removed the last element
            Array.Copy(arr, index + 1, arr, index, length: _count - index);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            arr[_count] = default!;
        }
    }

    /// <inheritdoc cref="RemoveAt(int)"/>
    public void RemoveAt(Index index)
    {
        RemoveAt(index.GetOffset(_count));
    }

    /// <summary>
    /// Removes a range of items, specifying a start index and the amount of items to remove.
    /// </summary>
    /// <param name="index"> The first index to remove at. </param>
    /// <param name="count"> The amount of items to remove. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="index"/> or <paramref name="count"/> are negative. </exception>
    /// <exception cref="ArgumentException"> <paramref name="index"/> and <paramref name="count"/> do not denote a valid range. </exception>
    public void RemoveRange(int index, int count)
    {
        if (index < 0)
            ThrowHelper.ArgumentOutOfRange_IndexMustBePositive();
        if (count < 0)
            ThrowHelper.ArgumentOutOfRange_CountMustBePositive();
        if (_count - index < count)
            ThrowHelper.Argument_CountOrOffsetInvalid();

        if (count > 0)
        {
            var arr = _array;
            Debug.Assert(arr != null);

            _count -= count;
            if (index < _count)
            {
                // We can skip moving the last elements if we removed the last elements
                Array.Copy(arr, index + count, arr, index, length: _count - index);
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(arr, _count, count);
            }
        }
    }

    /// <summary>
    /// Removes a range of items, specifying the indices of items to remove.
    /// </summary>
    /// <param name="range"> The index range to remove. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="range"/> does not denote a valid range. </exception>
    public void RemoveRange(Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(_count);
        RemoveRange(offset, length);
    }

    #endregion

    #region Insert

    /// <summary>
    /// Inserts an item into this list at the specified index.
    /// </summary>
    /// <param name="index"> The index to insert the item at. </param>
    /// <param name="item"> The item to add. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="index"/> is negative or greater than <see cref="Count"/>. </exception>
    public void Insert(int index, T item)
    {
        int count = _count;
        if ((uint)index > (uint)count)
            ThrowHelper.ArgumentOutOfRange_IndexMustBeLessOrEqual();

        var arr = EnsureCapacityUnchecked(count + 1);
        if ((uint)index < (uint)count)
        {
            // Copy the items starting "index" 1 further up
            Array.Copy(arr, index, arr, index + 1, length: count - index);
        }

        arr[index] = item;
        _count = count + 1;
    }

    /// <inheritdoc cref="Insert(int, T)"/>
    public void Insert(Index index, T item)
    {
        Insert(index.GetOffset(_count), item);
    }

    /// <summary>
    /// Inserts a range of items, starting at the specified index of this list.
    /// </summary>
    /// <param name="index"> The index to insert the items at. </param>
    /// <param name="items"> The items to add. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="index"/> is negative or greater than <see cref="Count"/>. </exception>
    public void InsertRange(int index, ReadOnlySpan<T> items)
    {
        int count = _count;
        if ((uint)index > (uint)count)
            ThrowHelper.ArgumentOutOfRange_IndexMustBeLessOrEqual();

        int newCount = count + items.Length;

        var arr = EnsureCapacityUnchecked(newCount);
        ThrowHelper.NullRefIfNull(arr);

        Array.Copy(arr, index, arr, index + items.Length, count - index);

        var selfSpan = arr.AsSpan();
        if (selfSpan.Overlaps(items, out int offset) && offset >= index)
        {
            // Handle "insert into self" correctly.

            // If the offset is less than or equal to the index, the items section wasn't clobbered.
            // In that case, we can use the branch we use for every other collection.
            //
            // That also means that, if the offset is the same as the index, we are already in the correct state.
            // In that case, the other branch would just copy a memory section to itself.

            if (offset > index)
            {
                // In case the offset is after the index, the original section has been shifted
                // back by `items.Length`, but luckily we can just take that and copy it to where it belongs.
                selfSpan.Slice(offset + items.Length, items.Length).CopyTo(selfSpan[index..]);
            }
        }
        else
        {
            items.CopyTo(selfSpan[index..]);
        }

        _count = newCount;
    }

    /// <inheritdoc cref="InsertRange(int, ReadOnlySpan{T})"/>
    public void InsertRange(Index index, ReadOnlySpan<T> items)
    {
        InsertRange(index.GetOffset(_count), items);
    }

    /// <inheritdoc cref="InsertRange(int, ReadOnlySpan{T})"/>
    /// <typeparam name="TDerived"> A derived type. </typeparam>
    public void InsertRange<TDerived>(int index, ReadOnlySpan<TDerived> items) where TDerived : class?, T
    {
        InsertRange(index, SpanEx<T>.CastUp(items));
    }

    /// <inheritdoc cref="InsertRange{TDerived}(int, ReadOnlySpan{TDerived})"/>
    public void InsertRange<TDerived>(Index index, ReadOnlySpan<TDerived> items) where TDerived : class?, T
    {
        InsertRange(index.GetOffset(_count), items);
    }

    /// <inheritdoc cref="InsertRange(int, ReadOnlySpan{T})"/>
    /// <exception cref="ArgumentNullException"> <paramref name="items"/> is <see langword="null"/>. </exception>
    public void InsertRange(int index, IEnumerable<T> items)
    {
        int count = _count;
        if ((uint)index > (uint)count)
            ThrowHelper.ArgumentOutOfRange_IndexMustBeLessOrEqual();

        if (items == null)
            ThrowHelper.ArgumentNull_Items();

        if (items is ICollection<T> coll)
        {
            int itemsCount = coll.Count;
            if (itemsCount == 0) return;

            // Don't modify '_count' until the end in case there is an exception
            // or 'this' and 'items' refer to the same boxed list.
            int newCount = count + itemsCount;
            var arr = EnsureCapacityUnchecked(newCount);
            Array.Copy(arr, index, arr, index + itemsCount, count - index);

            if (items is IValueList vl && vl.Array == arr)
            {
                // Handle "insert into self" correctly.
                Array.Copy(arr, 0, arr, index, index);
                Array.Copy(arr, index + itemsCount, arr, index + index, _count - index);
            }
            else
            {
                coll.CopyTo(arr, index);
            }

            _count = newCount;
        }
        else
        {
            foreach (var item in items)
            {
                Insert(index++, item);
            }
        }
    }

    /// <inheritdoc cref="InsertRange(int, IEnumerable{T})"/>
    public void InsertRange(Index index, IEnumerable<T> items)
    {
        InsertRange(index.GetOffset(_count), items);
    }

    #endregion

    /// <summary>
    /// Returns the first index of the given item based on default equality.
    /// </summary>
    /// <param name="item"> The item to search for. </param>
    /// <returns> The index, or -1 if it is not found. </returns>
    public readonly int IndexOf(T item)
    {
        int count = _count;
        return count != 0 ? Array.IndexOf(_array!, item, 0, count) : -1;
    }

    /// <summary>
    /// Returns the last index of the given item based on default equality.
    /// </summary>
    /// <param name="item"> The item to search for. </param>
    /// <returns> The index, or -1 if it is not found. </returns>
    public readonly int LastIndexOf(T item)
    {
        int count = _count;
        return count != 0 ? Array.LastIndexOf(_array!, item, 0, count) : -1;
    }

    /// <summary>
    /// Determines if the item is in the list based on default equality.
    /// </summary>
    /// <param name="item"> The item to search for. </param>
    /// <returns> If the item is found. </returns>
    public readonly bool Contains(T item)
    {
        int count = _count;
        return count != 0 && Array.IndexOf(_array!, item, 0, count) >= 0;
    }

    /// <summary>
    /// Ensures the capacity is sufficient to add <paramref name="count"/> items.
    /// </summary>
    /// <param name="count"> The amount of items to ensure space for. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="count"/> is negative. </exception>
    public void Reserve(int count)
    {
        if (count < 0)
            ThrowHelper.ArgumentOutOfRange_CountMustBePositive();

        EnsureCapacityUnchecked(_count + count);
    }

    /// <summary>
    /// Ensures the capacity is at least <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity"> The minimum capacity to allocate. </param>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="capacity"/> is negative. </exception>
    public void EnsureCapacity(int capacity)
    {
        // This method exists in part due to collection expressions (.NET 8+) and
        // may be called while constructing an instance of this type.

        if (capacity <= 0)
        {
            if (capacity < 0)
                ThrowHelper.ArgumentOutOfRange_CapacityMustBePositive();

            return;
        }

        if (_array == null)
        {
            _array = AllocateArrayChecked(capacity);
            return;
        }

        EnsureCapacityUnchecked(capacity);
    }

    /// <summary>
    /// Determines if any element is contained.
    /// </summary>
    public readonly bool Any()
    {
        return _count > 0;
    }

    /// <summary>
    /// Determines if any element matches the <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate"> The expression to match. </param>
    public readonly bool Any(Func<T, bool> predicate)
    {
        foreach (var item in this)
        {
            if (predicate(item))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if all elements match the <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate"> The expression to match. </param>
    public readonly bool All(Func<T, bool> predicate)
    {
        foreach (var item in this)
        {
            if (!predicate(item))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Clears the contents of this list without reducing its capacity.
    /// </summary>
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() && _count != 0)
        {
            // Only need to clear if there are references contained.
            Array.Clear(_array!, 0, _count);
        }
        _count = 0;
    }

    /// <summary>
    /// Whether the given lists are linked. This means their backing array is the same.
    /// </summary>
    /// <param name="other"> The other list to compare against. </param>
    /// <returns> Whether the backing arrays are the same. </returns>
    public readonly bool IsLinkedTo(ValueList<T, TAlloc> other)
    {
        return _array == other._array;
    }

    #region Casts

    /// <summary>
    /// Returns a <see cref="Span{T}"/> representing the active area of the list.
    /// Elements should not be added to or removed from the list while the span is in use.
    /// </summary>
    /// <returns> A span over this list. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan()
    {
        // A manual implementation, while "less safe", means we can elide
        // some checks that Span construction would otherwise do.
        // Specifically, we can avoid 2 checks:
        // 1. Array type check (for mutable spans)
        // 2. Start/Length == 0 check if array is null

        Span<T> result = default;

        var array = _array;
        if (array != null)
        {
            ValueList.ValidateArray(array);

            int count = _count;
            if ((uint)count > (uint)array.Length)
                ValueList.ThrowTornStruct();

            result = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(array), count);
        }

        return result;
    }

    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> representing the active area of the list.
    /// Elements should not be added to or removed from the list while the span is in use.
    /// </summary>
    /// <returns> A span over this list. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<T> AsReadOnlySpan()
    {
        return AsSpan();
    }

    /// <summary>
    /// Copies the contents of this list into a new array.
    /// </summary>
    /// <returns> An array containing the data in the current list. </returns>
    public readonly T[] ToArray()
    {
        // AsSpan().ToArray() works just as well, but this generates less assembly code, and more of it is shared across Ts.
        // Ultimately, this will call into the same copy routine, so any differences in perf will be negligible.

        int count = _count;
        if (count == 0) return [];

        var result = new T[count];
        Array.Copy(_array!, result, count);
        return result;
    }

    /// <summary>
    /// Copies the contents of this list into a new immutable array.
    /// </summary>
    /// <returns> An immutable array containing the data in the current list. </returns>
    public readonly ImmutableArray<T> ToImmutableArray()
    {
        // Wrap the result of the previous method.
        return ImmutableCollectionsMarshal.AsImmutableArray(ToArray());
    }

    #endregion

    /// <summary>
    /// Creates an independent copy of this instance with a separate backing storage.
    /// </summary>
    /// <returns> An independent copy. </returns>
    public readonly ValueList<T, TAlloc> Clone()
    {
        ValueList<T, TAlloc> copy = default;
        copy.AddRange(AsReadOnlySpan());
        return copy;
    }

    /// <summary>
    /// Creates an independent copy of this instance with a separate backing storage, using a different array allocator.
    /// </summary>
    /// <typeparam name="TNewAlloc"> The allocator to use for the new instance. </typeparam>
    /// <returns> An independent copy. </returns>
    public readonly ValueList<T, TNewAlloc> Clone<TNewAlloc>()
        where TNewAlloc : IArrayAllocator
    {
        ValueList<T, TNewAlloc> copy = default;
        copy.AddRange(AsReadOnlySpan());
        return copy;
    }

    /// <summary>
    /// Creates an enumerable that does not box this instance.
    /// </summary>
    /// <returns> An enumerable over the active list area. </returns>
    public readonly IEnumerable<T> ToEnumerable()
    {
        int count = _count;
        if (count == 0)
            return Enumerable.Empty<T>();

        var array = _array;
        return array!.Length == count ? array : array.Take(count);
    }

    /// <summary>
    /// Creates an enumerable that can be enumerated once and will dispose this list after use.
    /// This instance is moved there and reset immediately.
    /// </summary>
    /// <returns> An enumerable that may be iterated once. </returns>
    public IEnumerable<T> ToOnceEnumerable()
    {
        var enumerator = new OnceEnumerator(in this);
        this = default;
        return enumerator;
    }

    /// <summary>
    /// Returns an enumerator that can be used to iterate over this collection.
    /// </summary>
    /// <remarks>
    /// While the enumerator is in use, avoid adding or removing elements from the collection or doing anything that may change capacity.
    /// Doing so may lead to the enumerator reading from an array that has already been freed.
    /// </remarks>
    /// <returns> An enumerator for this list. </returns>
    public readonly ValueListEnumerator<T> GetEnumerator() => new ValueListEnumerator<T>(_array, _count);

    /// <summary>
    /// Deallocates the internal buffer and resets this instance.
    /// </summary>
    public void Dispose()
    {
        var arr = _array;
        if (arr != null)
        {
            this = default;
            TAlloc.Deallocate(arr);
        }
    }

    private T[] EnsureCapacityUnchecked(int size)
    {
        var arr = _array;
        if (arr == null || arr.Length < size)
        {
            arr = ExpandCapacity(size);
        }

        return arr;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private T[] ExpandCapacity(int size)
    {
        var arr = _array;
        if (arr == null)
        {
            size = Math.Max(ValueList.InitialSize, size);
            arr = AllocateArrayChecked(size);
            _array = arr;
        }
        else
        {
            // Get new array, copy over contents, replace current, delete old array
            // We need to retain the order to minimize problems in cases with exceptions

            size = Math.Max(arr.Length * 2, size);
            var newArr = AllocateArrayChecked(size);

            Array.Copy(arr, newArr, _count);
            _array = newArr;
            TAlloc.Deallocate(arr);
            arr = newArr;
        }

        return arr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T[] AllocateArrayChecked(int size)
    {
        var arr = TAlloc.Allocate<T>(size);

        // NewArrayAllocator is fully trusted to fulfill the contract
        // and is the most common instantiation, so we don't check it.
        //
        // Note that ArrayPoolAllocator is not fully trusted because arrays
        // returned to it may be bogus and could open up memory corruption.
        if (typeof(TAlloc) != typeof(NewArrayAllocator))
        {
            // Otherwise, if it can't be fully trusted,
            // we check the array at runtime and throw if it's incorrect.

            if (arr == null || arr.Length < size)
            {
                // The array provided must be non-null and large enough.
                ValueList.ThrowInvalidArraySize();
            }

            if (!typeof(T).IsValueType && arr.GetType() != typeof(T[]))
            {
                // Due to the unsafe code, we must get an array with exactly the right type,
                // unless T is a value type. This is the same requirements Span<T> has.
                ThrowHelper.ArrayTypeMismatch();
            }
        }

        // Paranoid debug assert
        ValueList.ValidateArray(arr);
        return arr;
    }

    #region Generic collection interfaces

    T IList<T>.this[int index] { readonly get => this[index]; set => this[index] = value; }
    readonly T IReadOnlyList<T>.this[int index] => this[index];

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly bool ICollection<T>.IsReadOnly => false;

    readonly void ICollection<T>.CopyTo(T[] array, int arrayIndex) => CopyTo(array, arrayIndex);

    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetClsEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetClsEnumerator();

    readonly ReadOnlySpan<T> IAsSpan<T>.AsSpan() => AsReadOnlySpan();

    #endregion

    #region Non-generic collection interfaces

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly Array? IValueList.Array => _array;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly bool IList.IsFixedSize => false;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly bool IList.IsReadOnly => false;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly bool ICollection.IsSynchronized => false;

    object? IList.this[int index] { readonly get => this[index]; set => this[index] = (T)value!; }

    int IList.Add(object? value) => AddObject(value);
    void IList.Insert(int index, object? value) => Insert(index, (T)value!);
    readonly bool IList.Contains(object? value) => VarHelper.IsCompatible<T>(value) && Contains((T)value!);
    readonly int IList.IndexOf(object? value) => VarHelper.IsCompatible<T>(value) ? IndexOf((T)value!) : -1;
    void IList.Remove(object? value) => RemoveObject(value);

    readonly void ICollection.CopyTo(Array array, int index) => CopyToObject(array, index);

    #endregion

    #region Intl for shared impl

    internal int AddObject(object? value)
    {
        int result = _count;
        Add((T)value!);
        return result;
    }

    internal void RemoveObject(object? value)
    {
        if (VarHelper.IsCompatible<T>(value))
        {
            Remove((T)value!);
        }
    }

    internal readonly void CopyTo(T[] array, int arrayIndex) => CollectionOfTImplHelper.CopyTo(_array, _count, array, arrayIndex);
    internal readonly void CopyToObject(Array array, int index) => CollectionNGImplHelper.CopyTo(_array, _count, array, index);

    internal readonly IEnumerator<T> GetClsEnumerator() => new StructEnumerator<T, ValueListEnumerator<T>>(GetEnumerator());

    #endregion

    private sealed class OnceEnumerator : IEnumerator<T>, IEnumerable<T>
    {
        private ValueListEnumerator<T> _enumerator;

        public OnceEnumerator(in ValueList<T, TAlloc> list)
        {
            _enumerator = list.GetEnumerator();
        }

        public T Current => _enumerator.Current;
        object? IEnumerator.Current => _enumerator.Current;

        public bool MoveNext() => _enumerator.MoveNext();

        public void Dispose()
        {
            var arr = _enumerator.Array;
            if (arr != null)
            {
                _enumerator = default;
                TAlloc.Deallocate(arr);
            }
        }

        public IEnumerator<T> GetEnumerator() => this;

        public void Reset() => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => this;
    }
}
