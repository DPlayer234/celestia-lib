using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib.Collections.Internal;

/// <summary>
/// Provides a simple struct that can be used as an array enumerators.
/// </summary>
/// <typeparam name="T"> The element type. </typeparam>
internal struct ArrayEnumerator<T> : IStructEnumerator<T>
{
    private readonly T[] _array;
    private int _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayEnumerator{T}"/> struct.
    /// </summary>
    /// <param name="array"> The array to enumerate. </param>
    public ArrayEnumerator(T[] array)
    {
        _array = array;
        _index = -1;
    }

    /// <summary> Gets the iterated array. </summary>
    public readonly T[] Array
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array;
    }

    /// <summary> Gets the current index in the iteration. </summary>
    public readonly int CurrentIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _index;
    }

    /// <summary> Gets the current element in the iteration. </summary>
    public readonly T Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array[_index];
    }

    /// <summary> Moves the iterator to the next item. </summary>
    /// <returns> Whether there is still an item. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_index < _array.Length;

    readonly T IStructEnumerator<T>.Current => Current;
}
