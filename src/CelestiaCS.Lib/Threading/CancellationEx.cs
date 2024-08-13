using System;
using System.Threading;
using System.Threading.Tasks;
using CelestiaCS.Lib.Threading.CompilerServices;

namespace CelestiaCS.Lib.Threading;

/// <summary>
/// Provides additional methods to operate with cancellations.
/// </summary>
public static class CancellationEx
{
    /// <summary>
    /// Creates a task that successfully completes when the <paramref name="token"/> is cancelled.
    /// </summary>
    /// <param name="token"> The token to wait for. </param>
    /// <returns> A task that completes when the <paramref name="token"/> is cancelled. </returns>
    /// <exception cref="ArgumentException"> The <paramref name="token"/> cannot be cancelled. </exception>
    public static Task CreateTaskOf(CancellationToken token)
    {
        AssertCanBeCanceled(token);

        if (token.IsCancellationRequested)
            return Task.CompletedTask;

        var taskSrc = new TaskCompletionSource();
        token.Register(t => ((TaskCompletionSource)t!).TrySetResult(), taskSrc);
        return taskSrc.Task;
    }

    /// <summary>
    /// Creates a task that successfully completes when the <paramref name="token"/> is cancelled.
    /// The result of the task will be the default value of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"> The type of the result. </typeparam>
    /// <param name="token"> The token to wait for. </param>
    /// <returns> A task that completes when the <paramref name="token"/> is cancelled. </returns>
    /// <exception cref="ArgumentException"> The <paramref name="token"/> cannot be cancelled. </exception>
    public static Task<T?> CreateTaskOf<T>(CancellationToken token)
    {
        AssertCanBeCanceled(token);

        if (token.IsCancellationRequested)
            return Task.FromResult<T?>(default);

        var taskSrc = new TaskCompletionSource<T?>();
        token.Register(t => ((TaskCompletionSource<T?>)t!).TrySetResult(default), taskSrc);
        return taskSrc.Task;
    }

    /// <summary>
    /// Waits for the <paramref name="token"/> to be cancelled.
    /// </summary>
    /// <param name="token"> The token to wait for. </param>
    /// <returns> An awaiter that may be used to wait on the cancellation. </returns>
    /// <exception cref="ArgumentException"> The <paramref name="token"/> cannot be cancelled. </exception>
    public static CancellationTokenAwaiter WaitForCancellationAsync(CancellationToken token)
    {
        AssertCanBeCanceled(token);
        return new CancellationTokenAwaiter(token);
    }

    /// <summary>
    /// Creates a <see cref="SharedCancellationToken"/> from a source.
    /// </summary>
    /// <param name="source"> The cancellation token source. </param>
    /// <returns> A shared cancellation token. </returns>
    public static SharedCancellationToken AsSharedToken(this CancellationTokenSource? source)
    {
        return new SharedCancellationToken(source);
    }

    private static void AssertCanBeCanceled(CancellationToken token)
    {
        if (!token.CanBeCanceled)
        {
            ThrowHelper.Argument(nameof(token), "Provided CancellationTokens must be cancellable.");
        }
    }
}
