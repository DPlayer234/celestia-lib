using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CelestiaCS.Lib.Threading.CompilerServices;
using CelestiaCS.Lib.Threading.Internal;

namespace CelestiaCS.Lib.Threading;

/// <summary>
/// Provides some common extensions for value-tasks.
/// </summary>
public static class ValueTaskExtensions
{
    /// <summary>
    /// Casts the task into one where the result is nullable.
    /// </summary>
    /// <typeparam name="T"> The type of the result. </typeparam>
    /// <param name="valueTask"> The task. </param>
    /// <returns> The same task. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<T?> AsNullable<T>(this ValueTask<T> valueTask) => valueTask!;

    /// <summary>
    /// Creates a new task that wraps the source task but omits the result.
    /// </summary>
    /// <typeparam name="T"> The type of the original result. </typeparam>
    /// <param name="valueTask"> The task. </param>
    /// <returns> The same task without a result. </returns>
    public static ValueTask AsVoid<T>(this ValueTask<T> valueTask)
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            // Fast path if Result is available and not faulted
            // Need to consume the result!
            _ = valueTask.Result;
            return ValueTask.CompletedTask;
        }

        return WithAwait(valueTask);

        static ValueTask WithAwait(in ValueTask<T> valueTask)
        {
            var vtSrc = CovariantValueTaskSource<T>.RentFor(valueTask);
            return new ValueTask(vtSrc, vtSrc.Token);
        }
    }

    /// <summary>
    /// Creates a new task whose result is reference cast from the source task.
    /// </summary>
    /// <typeparam name="TFrom"> The original result type. </typeparam>
    /// <typeparam name="TTo"> The final result type. </typeparam>
    /// <param name="valueTask"> The task. </param>
    /// <returns> The same task with a cast result. </returns>
    public static ValueTask<TTo> As<TFrom, TTo>(this ValueTask<TFrom> valueTask)
        where TFrom : class?, TTo
    {
        if (valueTask.IsCompletedSuccessfully)
        {
            // Fast path if Result is available and not faulted
            // Consume the Result and construct a new ValueTask<>
            return ValueTask.FromResult<TTo>(valueTask.Result);
        }

        return WithAwait(valueTask);

        static ValueTask<TTo> WithAwait(in ValueTask<TFrom> valueTask)
        {
            var vtSrc = CovariantValueTaskSource<TFrom>.RentFor(valueTask);
            return new ValueTask<TTo>(vtSrc, vtSrc.Token);
        }
    }

    /// <summary>
    /// To be used with <see langword="await"/> <see langword="using"/>:
    /// Delays awaiting a <see cref="ValueTask"/> until the end of the scope with a guarantee that it will be awaited.
    /// </summary>
    /// <remarks> This does not delay the actual execution of the task and it may already have finished by the time it is awaited. </remarks>
    /// <param name="valueTask"> The task to await later. </param>
    /// <returns> An async-disposable to wrap in a <see langword="using"/>. </returns>
    public static ValueTaskAsDisposable AfterAsync(this ValueTask valueTask)
    {
        return new ValueTaskAsDisposable(valueTask);
    }
}
