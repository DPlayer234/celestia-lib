using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace CelestiaCS.Lib.Threading;

/// <summary>
/// Provides a reusable class serving as an alternative to <see cref="Task.Delay(TimeSpan)"/>.
/// </summary>
/// <remarks>
/// This type is only disposable to allow canceling the underlying timer. It is acceptable to not dispose it.
/// </remarks>
public sealed class DelayTimer : IValueTaskSource, IDisposable
{
    private static readonly TimerCallback _timerCallback = EndTimeout;

    // A timer that is reused for every WaitAsync operation.
    private readonly ITimer _timer;

    private bool _isWaiting;
    private ManualResetValueTaskSourceCore<byte> _core;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelayTimer"/> class.
    /// </summary>
    public DelayTimer() : this(TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelayTimer"/> class.
    /// </summary>
    /// <remarks> The time provider to use. </remarks>
    public DelayTimer(TimeProvider timeProvider)
    {
        // `TimeProvider.System.CreateTimer` actually returns a thin wrapper around the internal `TimerQueueTimer`.
        // The regular `Timer` class wraps `TimerHolder`, which then in turn wraps `TimerQueueTimer` to retain legacy behavior with GCs.
        // However, this does mean that the timer returned by the system time provider actually allocates less, so we don't special-case it.

        ArgumentNullException.ThrowIfNull(timeProvider);
        _timer = timeProvider.CreatePausedTimer(_timerCallback, this);
    }

    /// <summary> Waits for the specified time and then completes the task. </summary>
    /// <remarks> Multiple concurrent waits are not supported. </remarks>
    /// <param name="delay"> The time to wait for. </param>
    /// <returns> A task representing the wait. </returns>
    public ValueTask WaitAsync(TimeSpan delay)
    {
        if (_isWaiting)
        {
            ThrowHelper.InvalidOperation($"{nameof(DelayTimer)}.{nameof(WaitAsync)} cannot be used concurrently.");
        }

        if (delay == TimeSpan.Zero)
        {
            return ValueTask.CompletedTask;
        }

        _isWaiting = true;
        _core.Reset();
        _timer.FireOnceAfter(delay);

        return new ValueTask(this, _core.Version);
    }

    /// <summary> Disposes the underlying timer. </summary>
    public void Dispose()
    {
        _timer.Dispose();
        Signal();
    }

    private void Signal()
    {
        // Reset the wait flag for setting the result, as setting the result
        // already runs the continuation which may call WaitAsync again.
        //
        // Ideally, we'd reset the flag in `GetResult` below. While entirely valid,
        // that can also invalidate this entire instance if the task is discarded.

        _isWaiting = false;
        _core.SetResult(0);
    }

    private static void EndTimeout(object? state)
    {
        ((DelayTimer)state!).Signal();
    }

    void IValueTaskSource.GetResult(short token)
        => _core.GetResult(token);

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
        => _core.GetStatus(token);

    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => _core.OnCompleted(continuation, state, token, flags);
}
