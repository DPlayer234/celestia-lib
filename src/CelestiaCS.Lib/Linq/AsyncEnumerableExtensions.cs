using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CelestiaCS.Lib.Collections;

namespace CelestiaCS.Lib.Linq;

public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Copies the source enumerable to a pooled, disposable buffer.
    /// This is useful to not iterate the source buffer anymore while avoiding most allocations.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="ct"> A token to check for cancellation. </param>
    /// <returns> The pooled, disposable buffer. </returns>
    public static ValueTask<PooledBuffer<T>> ToPooledBufferAsync<T>(this IAsyncEnumerable<T> source, CancellationToken ct = default)
    {
        return PooledBuffer<T>.FromAsync(source, ct);
    }

    /// <summary>
    /// Creates a <see cref="ValueList{T}"/> from an async-enumerable sequence.
    /// </summary>
    /// <typeparam name="T"> The type of items. </typeparam>
    /// <param name="source"> The source collection. </param>
    /// <param name="ct"> A token to check for cancellation. </param>
    /// <returns> A value list with the contents of the <paramref name="source"/>. </returns>
    public static async ValueTask<ValueList<T>> ToValueListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        ValueList<T> result = default;
        await foreach (var item in source.WithCancellation(ct))
        {
            result.Add(item);
        }

        return result;
    }
}
