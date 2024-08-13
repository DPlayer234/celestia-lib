using System;

namespace CelestiaCS.Lib.Memory;

/// <summary>
/// Defines that the collection may be read as a span.
/// </summary>
/// <typeparam name="T"> The type of the items. </typeparam>
public interface IAsSpan<T>
{
    /// <summary>
    /// Gets a span that represents the contents of this collection.
    /// </summary>
    ReadOnlySpan<T> AsSpan();
}
