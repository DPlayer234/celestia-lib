using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CelestiaCS.Lib.Threading;

/// <summary>
/// Provides some common extensions for tasks.
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Casts the task into one where the result is nullable.
    /// </summary>
    /// <typeparam name="T"> The type of the result. </typeparam>
    /// <param name="task"> The task. </param>
    /// <returns> The same task. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<T?> AsNullable<T>(this Task<T> task) => task!;

    /// <summary>
    /// Creates a <seealso cref="ValueTask"/> that wraps this task.
    /// </summary>
    /// <param name="task"> The task. </param>
    /// <returns> The same task wrapped in a <seealso cref="ValueTask"/>. </returns>
    public static ValueTask AsValueTask(this Task task) => new ValueTask(task);

    /// <summary>
    /// Creates a <seealso cref="ValueTask{TResult}"/> that wraps this task.
    /// </summary>
    /// <typeparam name="T"> The type of the result. </typeparam>
    /// <param name="task"> The task. </param>
    /// <returns> The same task wrapped in a <seealso cref="ValueTask{TResult}"/>. </returns>
    public static ValueTask<T> AsValueTask<T>(this Task<T> task) => new ValueTask<T>(task);

    /// <summary>
    /// Creates a <seealso cref="ValueTask"/> that wraps this task but voids the result.
    /// </summary>
    /// <typeparam name="T"> The type of the result. </typeparam>
    /// <param name="task"> The task. </param>
    /// <returns> The same task wrapped in a <seealso cref="ValueTask"/>. </returns>
    public static ValueTask AsValueTaskVoid<T>(this Task<T> task) => new ValueTask(task);
}
