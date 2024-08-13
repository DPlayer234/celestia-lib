using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// Provides some static helper methods for working with <see cref="ValueList{T}"/> and <see cref="ValueList{T, TAlloc}"/>.
/// </summary>
internal static class ValueList
{
    // Shared methods that are neither instance nor TAlloc dependent.

    internal const int InitialSize = 4;

    [DoesNotReturn]
    internal static void ThrowTornStruct()
    {
        throw new InvalidOperationException("The ValueList was torn. This is likely due to concurrent use from multiple threads.");
    }

    [DoesNotReturn]
    internal static void ThrowInvalidArraySize()
    {
        throw new InvalidOperationException("The array provided by TAlloc was null or smaller than requested. Larger arrays are allowed.");
    }

    /// <summary>
    /// Debug-only assert to ensure the array is not null and type is exactly matched.
    /// </summary>
    [Conditional("DEBUG")]
    internal static void ValidateArray<T>(T[] array)
    {
        Debug.Assert(
            array != null && (typeof(T).IsValueType || array.GetType() == typeof(T[])),
            "Array must be non-null and match the expected type exactly.");
    }
}

/// <summary>
/// Helper interface to allow value-lists to properly implement the <see cref="ICollection.SyncRoot"/> property without an additional field.
/// </summary>
/// <remarks>
/// This requires the list to be pre-boxed. <c>where T : ICollection</c> is unusual and even other situations that might want a sync-root likely start from a boxed instance.
/// </remarks>
internal interface IValueList : ICollection
{
    object ICollection.SyncRoot => this;

    Array? Array { get; }
}
