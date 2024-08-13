using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CelestiaCS.Lib.State;

/// <summary>
/// An abstraction to allow a factory to cache values with the expectancy that writes will happen very rarely while reads are common.
/// </summary>
/// <typeparam name="TKey"> The dictionary key. </typeparam>
/// <typeparam name="TValue"> The dictionary value. </typeparam>
public abstract class RareWriteFactoryCache<TKey, TValue> where TKey : notnull
{
    private readonly object _lock = new();

    // Write cache. Directly modified, but only when a lock is taken.
    // This may be the same as `_read`, in which case it needs to be copied before being modified.
    private Dictionary<TKey, TValue> _write;

    // Read cache. Never mutated itself but will be replaced entirely on updates.
    // This means it can always be read without taking any locks.
    private Dictionary<TKey, TValue> _read;

    /// <summary> Initializes a new empty instance of the <see cref="RareWriteFactoryCache{TKey, TValue}"/> class. </summary>
    /// <param name="comparer"> The key comparer to use. </param>
    public RareWriteFactoryCache(IEqualityComparer<TKey>? comparer = null)
    {
        _read = _write = new(comparer);
    }

    /// <summary> Gets the comparer used to determine equality of keys. </summary>
    public IEqualityComparer<TKey> Comparer => _read.Comparer;

    /// <summary> Helper property for correct usage of <see cref="_write"/>. </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Dictionary<TKey, TValue> WriteableWrite
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Debug.Assert(Monitor.IsEntered(_lock), "This property may only be used within a lock.");

            var write = _write;
            return write == _read
                ? (_write = new Dictionary<TKey, TValue>(write, write.Comparer))
                : write;
        }
    }

    /// <summary> Reads a value, possibly creating it if not found. </summary>
    /// <param name="key"> The key to look for. </param>
    /// <returns> The found or created value. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue Read(TKey key)
    {
        // Try to get it from the read cache first.
        // This should be fast and eventually always be hit and can be done with no locks.
        if (_read.TryGetValue(key, out TValue? result))
        {
            return result;
        }

        return ReadSlow(key);
    }

    /// <summary> Preemptively writes a value to the cache. </summary>
    /// <param name="key"> The key to write. </param>
    /// <param name="value"> The value to write. </param>
    /// <returns> An already present value for <paramref name="key"/> or <paramref name="value"/>. </returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public TValue Write(TKey key, TValue value)
    {
        TValue? result;

        lock (_lock)
        {
            // We try to use the existing value, only setting this one if it wasn't found.
            if (!_write.TryGetValue(key, out result))
            {
                WriteableWrite[key] = result = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Empties the cache.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            WriteableWrite.Clear();
            _read = _write;
        }
    }

    /// <summary> Creates a value from a key in the case of a miss. </summary>
    /// <remarks> Re-entry into the cache must be avoided. This method won't be invoked concurrently. </remarks>
    /// <param name="key"> The key that is asked for. </param>
    /// <returns> The created value. </returns>
    protected abstract TValue Create(TKey key);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private TValue ReadSlow(TKey key)
    {
        // If the current thread already holds the lock, this method was called recursively.
        // This isn't supported and indicates it comes from a `Create` implementation, so we fail here.
        if (Monitor.IsEntered(_lock))
            IntlThrowHelper.InvalidRecursion();

        TValue? result;
        lock (_lock)
        {
            // The idea here is this:
            // - If write already contains the type, we update the read cache.
            // - If it doesn't, we add the value to the write but don't yet update read.
            // This means that the only situations we end up here are when we either
            // request a key that has never been asked for or only been asked for once.
            // Further, we never lock on the read cache since it's only ever replaced.

            if (_write.TryGetValue(key, out result))
            {
                // If the value already existed, this is at least the 2nd miss for this key. So we replace the read cache.
                // This "no-clone" read cache update is the reason as to why `WriteableWrite` is needed:
                // After this, the `_write` needs to be cloned before it can be modified again.
                _read = _write;
            }
            else
            {
                // If it didn't exist, create the value and write it to the write cache, then return that value.
                // Notably, we don't reset or update the read cache: It will be updated if a read there misses but the data is already in the write cache.
                // This is so that multiple misses to new pieces of data don't cause a lot of cloning.
                WriteableWrite[key] = result = Create(key);
            }
        }

        return result;
    }
}

file sealed class IntlThrowHelper
{
    [DoesNotReturn]
    public static void InvalidRecursion()
    {
        throw new InvalidOperationException($"Recursive calls into {nameof(RareWriteFactoryCache<int, int>)} are not permitted.");
    }
}
