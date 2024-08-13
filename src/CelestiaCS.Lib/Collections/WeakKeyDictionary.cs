using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CelestiaCS.Lib.Collections.Internal;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// A simple, list-storage-based dictionary with weak keys. Lookups are done via a linear search.
/// </summary>
/// <remarks>
/// Keys are compared by reference. Entries might not be collected if the key is strongly reachable from the associated value.
/// </remarks>
/// <typeparam name="TKey"> The type of the keys. Must be a class but may be <see langword="null"/>. </typeparam>
/// <typeparam name="TValue"> The type of the values. </typeparam>
public sealed class WeakKeyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    where TKey : class?
{
    private ValueList<Entry> _store;

    /// <summary>
    /// Gets or sets the value associated with a key.
    /// </summary>
    /// <param name="key"> The key to look for. </param>
    /// <returns> The value for the associated key. </returns>
    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? value) ? value : ThrowKeyNotFound();
        set => TryAddIntl(key, value, allowUpdate: true);
    }

    /// <summary>
    /// Adds a key and value to the collection.
    /// </summary>
    /// <param name="key"> The key. </param>
    /// <param name="value"> The associated value. </param>
    public void Add(TKey key, TValue value)
    {
        if (!TryAddIntl(key, value, allowUpdate: false))
        {
            ThrowHelper.InvalidOperation("Key already exists.");
        }
    }

    /// <summary>
    /// Tries to add a key and value to the collection, failing if the key is already present.
    /// </summary>
    /// <param name="key"> The key. </param>
    /// <param name="value"> The associated value. </param>
    /// <returns> Whether a pair was added. </returns>
    public bool TryAdd(TKey key, TValue value)
    {
        return TryAddIntl(key, value, allowUpdate: false);
    }

    /// <summary>
    /// Removes a key and its corresponding value from the collection.
    /// </summary>
    /// <param name="key"> The key. </param>
    /// <returns> If the item was removed. </returns>
    public bool Remove(TKey key)
    {
        ref Entry pair = ref GetEntryRef(key);
        if (Unsafe.IsNullRef(ref pair))
        {
            return false;
        }
        else
        {
            // If found, wipe the entry
            pair = default;
            return true;
        }
    }

    /// <summary>
    /// Tries to get the value associated with a key.
    /// </summary>
    /// <param name="key"> The key to look for. </param>
    /// <param name="value"> The value for the associated key. </param>
    /// <returns> If the key was found. </returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        ref Entry pair = ref GetEntryRef(key);
        if (Unsafe.IsNullRef(ref pair))
        {
            value = default;
            return false;
        }
        else
        {
            value = pair.Value;
            return true;
        }
    }

    /// <summary>
    /// Gets the value associated with a key, or <see langword="default"/> if not found.
    /// </summary>
    /// <param name="key"> The key to look for. </param>
    /// <returns> The found value or <see langword="default"/>. </returns>
    public TValue? GetValueOrDefault(TKey key)
    {
        ref Entry pair = ref GetEntryRef(key);
        if (Unsafe.IsNullRef(ref pair)) return default;
        return pair.Value;
    }

    /// <summary>
    /// Determines if there is a value for a specified key.
    /// </summary>
    /// <param name="key"> The key to look for. </param>
    /// <returns> If the key was found. </returns>
    public bool ContainsKey(TKey key)
    {
        return !Unsafe.IsNullRef(ref GetEntryRef(key));
    }

    /// <summary>
    /// Removes all keys and values from the collection.
    /// </summary>
    public void Clear()
    {
        _store.Clear();
    }

    /// <summary>
    /// Whether this dictionary has any active key-value pairs.
    /// </summary>
    /// <returns> Whether there is anything. </returns>
    public bool Any()
    {
        foreach (ref Entry pair in _store.AsSpan())
        {
            var kpKey = pair.Key;
            if (kpKey != null && kpKey.TryGetTarget(out _))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new Enumerator(this);

    private bool TryAddIntl(TKey key, TValue value, bool allowUpdate)
    {
        ref Entry pair = ref GetEntryRef(key);
        if (Unsafe.IsNullRef(ref pair))
        {
            // Reassign empty storage space
            pair = ref GetEmptyEntryRef();
            pair.Value = value;
            if (pair.Key is { } wrKey) wrKey.SetTarget(key);
            else pair.Key = new WeakReference<TKey>(key);
            return true;
        }
        else if (allowUpdate)
        {
            // Override value in the key-pair
            pair.Value = value;
            return true;
        }
        else
        {
            return false;
        }
    }

    private ref Entry GetEmptyEntryRef()
    {
        foreach (ref Entry pair in _store.AsSpan())
        {
            var kpKey = pair.Key;
            if (kpKey == null || !kpKey.TryGetTarget(out _))
            {
                return ref pair;
            }
        }

        _store.Add(default);
        return ref _store[^1];
    }

    private ref Entry GetEntryRef(TKey key)
    {
        foreach (ref Entry pair in _store.AsSpan())
        {
            var kpKey = pair.Key;
            if (kpKey != null &&
                kpKey.TryGetTarget(out TKey? keyTarget) &&
                ReferenceEquals(key, keyTarget))
            {
                return ref pair;
            }
        }

        return ref Unsafe.NullRef<Entry>();
    }

    private IEnumerator<KeyValuePair<TKey, TValue>> GetClsEnumerator() => new StructEnumerator<KeyValuePair<TKey, TValue>, Enumerator>(GetEnumerator());

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetClsEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetClsEnumerator();

    private static TValue ThrowKeyNotFound() => throw new KeyNotFoundException();

    public struct Enumerator : IStructEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly WeakKeyDictionary<TKey, TValue> _dict;
        private int _index;

        public Enumerator(WeakKeyDictionary<TKey, TValue> dict)
        {
            _dict = dict;
            _index = -1;
            Current = default!;
        }

        public KeyValuePair<TKey, TValue> Current { get; private set; }

        public bool MoveNext()
        {
            var span = _dict._store.AsSpan();
            int index = _index + 1;
            while ((uint)index < (uint)span.Length)
            {
                ref Entry pair = ref span[index];
                var kpKey = pair.Key;
                if (kpKey != null && kpKey.TryGetTarget(out TKey? key))
                {
                    Current = new(key, pair.Value);
                    _index = index;
                    return true;
                }

                index += 1;
            }

            _index = index;
            return false;
        }
    }

    private struct Entry
    {
        public WeakReference<TKey>? Key;
        public TValue Value;
    }
}
