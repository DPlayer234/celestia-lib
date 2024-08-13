using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace CelestiaCS.Lib.Threading;

/// <summary>
/// Provides a manually completable <seealso cref="ValueTask"/>.
/// </summary>
public sealed class ValueTaskCompletionSource : IValueTaskSource
{
    private SetOnce _completionLock;
    private ManualResetValueTaskSourceCore<byte> _core;

    /// <summary>
    /// The task this source completes.
    /// </summary>
    public ValueTask Task
        => new ValueTask(this, _core.Version);

    /// <summary>
    /// Tries to transition the task into a successful completion state and setting its result.
    /// </summary>
    /// <returns> If the operation was successful. </returns>
    public bool TrySetResult()
    {
        if (_completionLock.TrySet())
        {
            _core.SetResult(0);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to transition the task into a faulted completion state and setting its exception.
    /// </summary>
    /// <param name="exception"> The exception to set. </param>
    /// <returns> If the operation was successful. </returns>
    public bool TrySetException(Exception exception)
    {
        if (_completionLock.TrySet())
        {
            _core.SetException(exception);
            return true;
        }

        return false;
    }

    public void Reset()
    {
        _completionLock = default;
        _core.Reset();
    }

    void IValueTaskSource.GetResult(short token)
        => _core.GetResult(token);

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
        => _core.GetStatus(token);

    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => _core.OnCompleted(continuation, state, token, flags);
}

/// <summary>
/// Provides a manually completable <seealso cref="ValueTask{TResult}"/>.
/// </summary>
/// <typeparam name="TResult"> The type of the result value. </typeparam>
public sealed class ValueTaskCompletionSource<TResult> : IValueTaskSource<TResult>
{
    private SetOnce _completionLock;
    private ManualResetValueTaskSourceCore<TResult> _core;

    /// <summary>
    /// The task this source completes.
    /// </summary>
    public ValueTask<TResult> Task
        => new ValueTask<TResult>(this, _core.Version);

    /// <summary>
    /// Tries to transition the task into a successful completion state and setting its result.
    /// </summary>
    /// <param name="result"> The result to set. </param>
    /// <returns> If the operation was successful. </returns>
    public bool TrySetResult(TResult result)
    {
        if (_completionLock.TrySet())
        {
            _core.SetResult(result);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to transition the task into a faulted completion state and setting its exception.
    /// </summary>
    /// <param name="exception"> The exception to set. </param>
    /// <returns> If the operation was successful. </returns>
    public bool TrySetException(Exception exception)
    {
        if (_completionLock.TrySet())
        {
            _core.SetException(exception);
            return true;
        }

        return false;
    }

    public void Reset()
    {
        _completionLock = default;
        _core.Reset();
    }

    TResult IValueTaskSource<TResult>.GetResult(short token)
        => _core.GetResult(token);

    ValueTaskSourceStatus IValueTaskSource<TResult>.GetStatus(short token)
        => _core.GetStatus(token);

    void IValueTaskSource<TResult>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => _core.OnCompleted(continuation, state, token, flags);
}
