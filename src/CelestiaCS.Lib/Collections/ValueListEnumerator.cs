using CelestiaCS.Lib.Collections.Internal;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// An enumerator that can be used to iterate over a <see cref="ValueList{T}"/> or <see cref="ValueList{T, TAlloc}"/>.
/// </summary>
/// <typeparam name="T"> The type of items to hold. </typeparam>
public struct ValueListEnumerator<T> : IStructEnumerator<T>
{
    // Supports both value-list types with the same enumerator.
    // This is basically just an enumerator for an "array slice from start".

    // PERF: Using this type (`foreach` over ValueList directly) will emit a range check against the array on every iteration.
    // Benchmarks show that this has no to little impact on the performance over first casting the list to a span, and emits
    // slightly smaller code, so direct use of the foreach pattern is preferred.
    //
    // Manually enumerating (`for (int i = 0, ...`) respects changes to the list but appears performs worse in simple test cases.
    // Cast to a span if you need the index as well and don't modify the list.

    private readonly T[]? _array;
    private readonly int _count;
    private int _index;

    internal ValueListEnumerator(T[]? array, int count)
    {
        _array = array;
        _count = count;
        _index = -1;
    }

    // Needed for OnceEnumerator
    internal readonly T[]? Array => _array;

    /// <summary>
    /// Gets a reference to the current item.
    /// </summary>
    // PERF: Assume any use before MoveNext() or after it returns false is UB. Just don't read memory OOB.
    // Also, a `ref readonly` return already skips the variance check so we don't need unsafe code.
    public readonly ref readonly T Current => ref _array![_index];

    /// <summary>
    /// Tries to move to the next item in the enumeration.
    /// </summary>
    /// <returns> If there is another item. </returns>
    public bool MoveNext() => ++_index < _count;

    // Since we don't need a ref here, just directly index the array
    readonly T IStructEnumerator<T>.Current => _array![_index];
}
