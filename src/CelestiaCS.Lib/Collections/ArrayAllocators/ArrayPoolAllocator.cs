using System.Buffers;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib.Collections.ArrayAllocators;

/// <summary>
/// Allocates arrays using <seealso cref="ArrayPool{T}.Shared"/>.
/// </summary>
public readonly struct ArrayPoolAllocator : IArrayAllocator
{
    /// <inheritdoc/>
    public static T[] Allocate<T>(int minimumSize)
    {
        return ArrayPool<T>.Shared.Rent(minimumSize);
    }

    /// <inheritdoc/>
    public static void Deallocate<T>(T[] array)
    {
        bool clearArray = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        ArrayPool<T>.Shared.Return(array, clearArray);
    }
}
