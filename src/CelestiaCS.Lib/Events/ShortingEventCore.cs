using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace CelestiaCS.Lib.Events;

/// <summary>
/// Base type for event arguments that can be short-circuited.
/// </summary>
public class ShortingEventArgs
{
    /// <summary> Whether the event was handled already. </summary>
    public bool IsHandled { get; set; }
}

/// <summary>
/// A core struct for defining events that can be short-circuited.
/// </summary>
/// <typeparam name="T"> The type of the arguments. </typeparam>
[DebuggerDisplay("ShortingEventCore: HandlerCount = {HandlerCount}")]
public struct ShortingEventCore<T>
    where T : ShortingEventArgs
{
    private ImmutableArray<Action<T>> _events;
    private readonly int HandlerCount => _events is var e && !e.IsDefault ? e.Length : 0;

    /// <summary>
    /// Adds an event handler to the invocation list.
    /// </summary>
    /// <param name="invocation"> The handler to add. </param>
    public void Add(Action<T> invocation)
    {
        ImmutableInterlocked.Update(ref _events, IntlShared.Update<Action<T>>.Add, invocation);
    }

    /// <summary>
    /// Removes an event handler from the invocation list.
    /// </summary>
    /// <param name="invocation"> The handler to remove. </param>
    public void Remove(Action<T> invocation)
    {
        ImmutableInterlocked.Update(ref _events, IntlShared.Update<Action<T>>.Remove, invocation);
    }

    /// <summary>
    /// Invokes the event.
    /// </summary>
    /// <param name="args"> The event arguments to pass to all handlers. </param>
    public readonly bool Invoke(T args)
    {
        var events = _events;
        if (!events.IsDefaultOrEmpty)
        {
            foreach (var item in events)
            {
                if (args.IsHandled)
                    return true;

                item.Invoke(args);
            }
        }

        return args.IsHandled;
    }
}
