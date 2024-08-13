using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace CelestiaCS.Lib;

/// <summary>
/// A padded object. The padding is determined based on processor cache lines.
/// </summary>
/// <remarks>
/// Based on: https://source.dot.net/#System.Private.CoreLib/Padding.cs
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct PaddedReference
{
    // This type is not generic to avoid extra instantiations.

    /// <summary> The actual value. </summary>
    public object? Object;
    private readonly Padding _padding;

    [StructLayout(LayoutKind.Sequential)]
    private struct Padding
    {
        // 60 bytes on 32-bit, 120 bytes on 64-bit
        public nuint _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14;
    }
}

/// <summary>
/// A static cache that uses both thread-local storage and per-core storage for pooling.
/// At most 1 instance can be stored per thread and per core.
/// </summary>
/// <remarks>
/// Wrap the <see cref="Rent"/> and <see cref="Return"/> methods within other methods that handle your surrounding logic,
/// such as resetting returned objects.
/// </remarks>
/// <typeparam name="T"> The type of the cached items. </typeparam>
public static class TlsPerCoreCache<T>
    where T : class
{
    /// <inheritdoc cref="TlsPerCoreCache{T, TMarker}.Rent"/>
    public static T? Rent() => TlsPerCoreCache<T, T>.Rent();

    /// <inheritdoc cref="TlsPerCoreCache{T, TMarker}.Return(T)"/>
    public static void Return(T inst) => TlsPerCoreCache<T, T>.Return(inst);
}

// This class is based on the implementation for the caching of PoolingAsyncValueTaskMethodBuilder
/// <summary>
/// A static cache that uses both thread-local storage and per-core storage for pooling.
/// At most 1 instance can be stored per thread and per core.
/// </summary>
/// <remarks>
/// Wrap the <see cref="Rent"/> and <see cref="Return"/> methods within other methods that handle your surrounding logic,
/// such as resetting returned objects.
/// </remarks>
/// <typeparam name="T"> The type of the cached items. </typeparam>
/// <typeparam name="TMarker"> A use marker. Used to avoid sharing caches. </typeparam>
public static class TlsPerCoreCache<T, TMarker>
    where T : class
    where TMarker : class

    // Generics constrained to classes:
    // T => Cannot really pool structs
    // TMarker => Avoid unneeded generic overhead
{
    // Stores one item per processor core
    private static readonly PaddedReference[] _perCoreCache = new PaddedReference[Environment.ProcessorCount];

    // Store one item per thread
    [ThreadStatic]
    private static T? _tlsCache;

    /// <summary>
    /// Tries to rent an item from the cache. Returns <see langword="null"/> if no cached item was present.
    /// </summary>
    /// <returns> An item from the cache. </returns>
    public static T? Rent()
    {
        // First: Attempt Thread-Local
        T? inst = _tlsCache;
        if (inst is not null)
        {
            _tlsCache = null;
        }
        else
        {
            // Otherwise, try to get the value from the core slot.
            ref T? slot = ref GetPerCoreCacheSlot();
            if (slot is not null)
            {
                inst = Interlocked.Exchange(ref slot, null);
            }
        }

        return inst;
    }

    /// <summary>
    /// Returns an item to the cache.
    /// </summary>
    /// <remarks>
    /// Make sure to reset instances either when returning or renting.
    /// </remarks>
    /// <param name="inst"> The rented instance to return. </param>
    public static void Return(T inst)
    {
        // Store it into the Thread-Local if that's empty
        if (_tlsCache is null)
        {
            _tlsCache = inst;
        }
        else
        {
            // Otherwise, try to write it to the core slot
            ref T? slot = ref GetPerCoreCacheSlot();
            if (slot is null)
            {
                Volatile.Write(ref slot, inst);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref T? GetPerCoreCacheSlot()
    {
        Debug.Assert(_perCoreCache.Length == Environment.ProcessorCount);
        int i = (int)((uint)Thread.GetCurrentProcessorId() % (uint)Environment.ProcessorCount);

#if DEBUG
        object? cacheValue = _perCoreCache[i].Object;
        Debug.Assert(cacheValue is null or T, $"Expected null or {typeof(T).Name}, got '{cacheValue}'.");
#endif

        return ref Unsafe.As<object?, T?>(ref _perCoreCache[i].Object);
    }
}
