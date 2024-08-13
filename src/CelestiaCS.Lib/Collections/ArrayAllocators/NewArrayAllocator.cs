namespace CelestiaCS.Lib.Collections.ArrayAllocators;

/// <summary>
/// Allocates new arrays using <see langword="new"/> T[].
/// </summary>
public readonly struct NewArrayAllocator : IArrayAllocator
{
    /// <inheritdoc/>
    public static T[] Allocate<T>(int minimumSize)
    {
        // We could use GC.AllocateUninitializedArray, but 90% of the time,
        // we only need its small array fast path so we may as well save us
        // the extra generated code for this.
        return new T[minimumSize];
    }

    /// <summary>
    /// Does nothing. Arrays are not deallocated in this implementation.
    /// </summary>
    /// <typeparam name="T"> The type of the array items. </typeparam>
    /// <param name="array"> The array to deallocate. </param>
    public static void Deallocate<T>(T[] array)
    {
    }
}
