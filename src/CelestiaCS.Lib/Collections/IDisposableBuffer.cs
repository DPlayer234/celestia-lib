using System;
using System.Collections.Generic;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// Represents a read-only, disposable buffer of some kind.
/// </summary>
/// <typeparam name="T"> The type of items. </typeparam>
public interface IDisposableBuffer<T> : IReadOnlyCollection<T>, IDisposable
{
}
