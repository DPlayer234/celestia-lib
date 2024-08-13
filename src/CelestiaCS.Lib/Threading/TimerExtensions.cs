using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CelestiaCS.Lib.Threading;

/// <summary>
/// Provides extension methods for timers and time providers.
/// </summary>
public static class TimerExtensions
{
    /// <summary> Creates a new <see cref="ITimer"/> instance in a stopped state. </summary>
    /// <param name="timeProvider"> The time provider to use. </param>
    /// <param name="callback"> The callback to fire when the timer elapses. </param>
    /// <param name="state"> The state to pass to the timer. May be null. </param>
    /// <returns> The newly created timer. </returns>
    /// <exception cref="ArgumentNullException"> <paramref name="callback"/> is null. </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ITimer CreatePausedTimer(this TimeProvider timeProvider, TimerCallback callback, object? state = null)
    {
        return timeProvider.CreateTimer(callback, state, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary> Sets the timer to no longer fire. </summary>
    /// <param name="timer"> The timer to change. </param>
    /// <returns> Whether the timer was successfully updated. </returns>
    public static bool Cancel(this ITimer timer)
    {
        return timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary> Sets the timer to fire once after the <paramref name="delay"/> has passed. </summary>
    /// <param name="timer"> The timer to change. </param>
    /// <param name="delay"> The time to wait for before the fire. </param>
    /// <returns> Whether the timer was successfully updated. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="delay"/> is invalid. </exception>
    public static bool FireOnceAfter(this ITimer timer, TimeSpan delay)
    {
        return timer.Change(delay, Timeout.InfiniteTimeSpan);
    }

    /// <summary> Sets the timer to fire every after every <paramref name="interval"/> has passed. </summary>
    /// <remarks> The first fire will also happen after the <paramref name="interval"/> time, not immediately. </remarks>
    /// <param name="timer"> The timer to change. </param>
    /// <param name="interval"> The interval between fires. </param>
    /// <returns> Whether the timer was successfully updated. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="interval"/> is invalid. </exception>
    public static bool FireEvery(this ITimer timer, TimeSpan interval)
    {
        return timer.Change(interval, interval);
    }
}
