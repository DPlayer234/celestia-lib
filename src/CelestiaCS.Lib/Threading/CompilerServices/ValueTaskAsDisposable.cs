using System;
using System.Threading.Tasks;

namespace CelestiaCS.Lib.Threading.CompilerServices;

/// <summary>
/// Wraps a task as the value of an <see cref="IAsyncDisposable"/>.
/// </summary>
/// <param name="task"> The task to use. </param>
public readonly struct ValueTaskAsDisposable(ValueTask task) : IAsyncDisposable
{
    /// <inheritdoc/>
    public ValueTask DisposeAsync() => task;
}
