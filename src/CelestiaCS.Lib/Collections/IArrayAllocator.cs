namespace CelestiaCS.Lib.Collections;

/// <summary>
/// Defines a static interface for creating and destroying arrays of any type.
/// </summary>
public interface IArrayAllocator
{
    /// <summary>
    /// Allocates an array with at least the given size.
    /// </summary>
    /// <typeparam name="T"> The type of the array items. </typeparam>
    /// <param name="minimumSize"> The minimum array size. </param>
    /// <returns> The created array. </returns>
    static abstract T[] Allocate<T>(int minimumSize);

    /// <summary>
    /// Deallocates the array.
    /// </summary>
    /// <typeparam name="T"> The type of the array items. </typeparam>
    /// <param name="array"> The array to deallocate. </param>
    static abstract void Deallocate<T>(T[] array);
}
