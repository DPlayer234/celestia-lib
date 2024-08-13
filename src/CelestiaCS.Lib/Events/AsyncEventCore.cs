using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CelestiaCS.Lib.Events;

/// <summary>
/// Defines a callback for an event based on <seealso cref="AsyncEventCore{T}"/>.
/// </summary>
/// <typeparam name="T"> The type of the arguments. </typeparam>
/// <param name="args"> The arguments. </param>
/// <returns> A task that represents the pending operation. </returns>
public delegate Task AsyncEventHandler<T>(T args)
    where T : class;

/// <summary>
/// A core struct for defining asynchronously executed events.
/// </summary>
/// <typeparam name="T"> The type of the arguments. </typeparam>
[DebuggerDisplay("AsyncEventCore: HandlerCount = {HandlerCount}")]
public struct AsyncEventCore<T>
    where T : class
{
    private ImmutableArray<AsyncEventHandler<T>> _events;
    private readonly int HandlerCount => _events is var e && !e.IsDefault ? e.Length : 0;

    /// <summary>
    /// Adds an event handler to the invocation list.
    /// </summary>
    /// <param name="invocation"> The handler to add. </param>
    public void Add(AsyncEventHandler<T> invocation)
    {
        ImmutableInterlocked.Update(ref _events, IntlShared.Update<AsyncEventHandler<T>>.Add, invocation);
    }

    /// <summary>
    /// Removes an event handler from the invocation list.
    /// </summary>
    /// <param name="invocation"> The handler to remove. </param>
    public void Remove(AsyncEventHandler<T> invocation)
    {
        ImmutableInterlocked.Update(ref _events, IntlShared.Update<AsyncEventHandler<T>>.Remove, invocation);
    }

    /// <summary>
    /// Asynchronously invokes the event.
    /// </summary>
    /// <param name="args"> The event arguments to pass to all handlers. </param>
    /// <returns> A task that represents the pending operations. </returns>
    public readonly Task InvokeAsync(T args)
    {
        var events = _events;
        if (!events.IsDefaultOrEmpty)
        {
            if (events.Length == 1)
            {
                // Special case exactly 1 event to avoid extra allocations.
                return events[0].Invoke(args);
            }

            Task[] arrays = new Task[events.Length];
            for (int i = 0; i < events.Length; i++)
            {
                arrays[i] = events[i].Invoke(args);
            }

            // CMBK: .NET 9 (?) use span-based overload
            return Task.WhenAll(arrays);
        }

        return Task.CompletedTask;
    }
}
