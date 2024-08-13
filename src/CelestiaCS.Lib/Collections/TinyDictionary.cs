using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CelestiaCS.Lib.Collections.Internal;

namespace CelestiaCS.Lib.Collections;

/// <summary>
/// A list-based dictionary with O(n) lookup. Preferable to use for dictionaries that are relatively small.
/// </summary>
/// <typeparam name="TKey"> The type of the keys. </typeparam>
/// <typeparam name="TValue"> The type of the values. </typeparam>
public class TinyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    private readonly IEqualityComparer<TKey>? _equalityComparer;

    // This gets managed in a way where there are no gaps in the data.
    // That may involve reordering elements if something is removed.
    private ValueList<Entry> _store;

    /// <summary>
    /// Initializes a new instance for the <see cref="TinyDictionary{TKey, TValue}"/> class with the default equality comparer.
    /// </summary>
    public TinyDictionary()
    {
        if (!typeof(TKey).IsValueType)
        {
            // We ensure that we always hold a comparer for reference types
            // since EqualityComparer<T>.Default doesn't get devirtualized anyways
            _equalityComparer = EqualityComparer<TKey>.Default;
        }
    }

    /// <summary>
    /// Initializes a new instance for the <see cref="TinyDictionary{TKey, TValue}"/> class with a custom equality comparer.
    /// </summary>
    /// <param name="equalityComparer"> The equality comparer to use. </param>
    public TinyDictionary(IEqualityComparer<TKey>? equalityComparer)
    {
        if (typeof(TKey).IsValueType)
        {
            if (equalityComparer != EqualityComparer<TKey>.Default)
            {
                // For value types, we want to ensure the default does get devirtualized,
                // so we make sure that we don't store it if the default is explicitly passed.
                equalityComparer = null;
            }
        }
        else
        {
            // We ensure that we always hold a comparer for reference types
            // since EqualityComparer<T>.Default doesn't get devirtualized anyways
            equalityComparer ??= EqualityComparer<TKey>.Default;
        }

        _equalityComparer = equalityComparer;
    }

    /// <summary>
    /// Gets the count of entries currently in the dictionary.
    /// </summary>
    public int Count => _store.Count;

    /// <summary>
    /// The equality comparer in use by this dictionary.
    /// </summary>
    public IEqualityComparer<TKey> EqualityComparer => _equalityComparer ?? EqualityComparer<TKey>.Default;

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
    /// Removes a key and its corresponding value from the collection.
    /// </summary>
    /// <param name="key"> The key. </param>
    /// <returns> If the item was removed. </returns>
    public bool Remove(TKey key)
    {
        ref Entry pair = ref FindEntry(key);
        if (Unsafe.IsNullRef(ref pair))
        {
            return false;
        }
        else
        {
            // If found, override the entry with the last one
            // and remove the now duplicate entry from the end
            int index = _store.Count - 1;
            pair = _store[index];
            _store.RemoveAt(index);
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
        ref Entry pair = ref FindEntry(key);
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
    /// Gets the value associated with the key if present.
    /// Otherwise, returns the default value for <typeparamref name="TValue"/>.
    /// </summary>
    /// <param name="key"> The key to look for. </param>
    /// <returns> The associated value or default. </returns>
    public TValue? GetValueOrDefault(TKey key)
    {
        ref Entry pair = ref FindEntry(key);
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
        return !Unsafe.IsNullRef(ref FindEntry(key));
    }

    /// <summary>
    /// Removes all keys and values from the collection.
    /// </summary>
    public void Clear()
    {
        _store.Clear();
    }

    /// <summary>
    /// Gets the reference to the value associated with a key.
    /// If the key isn't already present, adds default first.
    /// </summary>
    /// <param name="key"> The key to look for. </param>
    /// <returns> The reference to the associated value. </returns>
    public ref TValue? GetValueRefOrAddDefault(TKey key)
    {
        ref Entry pair = ref FindEntry(key);
        if (Unsafe.IsNullRef(ref pair))
        {
            // Add a new entry and return it by ref
            _store.Add(new Entry { Key = key });
            return ref _store[^1].Value!;
        }

        return ref pair.Value!;
    }

    /// <summary>
    /// Gets the reference to the value associated with a key.
    /// If the key isn't present, returns a null-ref.
    /// </summary>
    /// <param name="key"> The key to look for. </param>
    /// <returns> The reference to the associated value or a null-ref. </returns>
    public ref TValue GetValueRefOrNullRef(TKey key)
    {
        ref Entry pair = ref FindEntry(key);
        if (Unsafe.IsNullRef(ref pair))
            return ref Unsafe.NullRef<TValue>();

        return ref pair.Value;
    }

    /// <summary> Creates an array with the contents of this dictionary. </summary>
    /// <returns> An array copy. </returns>
    public KeyValuePair<TKey, TValue>[] ToArray()
    {
        var span = _store.AsReadOnlySpan();
        if (span.IsEmpty) return [];

        var res = new KeyValuePair<TKey, TValue>[span.Length];
        for (int i = 0; i < res.Length; i++)
        {
            res[i] = span[i].ToKeyValuePair();
        }

        return res;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new Enumerator(this);

    private bool TryAddIntl(TKey key, TValue value, bool allowUpdate)
    {
        ref Entry pair = ref FindEntry(key);
        if (Unsafe.IsNullRef(ref pair))
        {
            // Add new entry:
            _store.Add(new Entry { Key = key, Value = value });
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

    private ref Entry FindEntry(TKey key)
    {
        IEqualityComparer<TKey>? comparer = _equalityComparer;
        if (typeof(TKey).IsValueType && comparer == null)
        {
            // Make use of devirtualizing the default equality comparer for
            // value types. Does not work for reference types.
            foreach (ref Entry pair in _store.AsSpan())
            {
                if (EqualityComparer<TKey>.Default.Equals(key, pair.Key))
                {
                    return ref pair;
                }
            }
        }
        else
        {
            // The constructor should ensure that for 'TKey : class' at least the default comparer is set.
            Debug.Assert(comparer != null, "Reference types should always get an EqualityComparer.");

            foreach (ref Entry pair in _store.AsSpan())
            {
                if (comparer.Equals(key, pair.Key))
                {
                    return ref pair;
                }
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
        private readonly TinyDictionary<TKey, TValue> _dict;
        private int _index;

        public Enumerator(TinyDictionary<TKey, TValue> dict)
        {
            _dict = dict;
            _index = 0;
            Current = default!;
        }

        public KeyValuePair<TKey, TValue> Current { get; private set; }

        public bool MoveNext()
        {
            var store = _dict._store.AsSpan();
            int index = _index;
            if ((uint)index < (uint)store.Length)
            {
                Current = store[index].ToKeyValuePair();
                _index = index + 1;
                return true;
            }

            return false;
        }
    }

    private struct Entry
    {
        public TKey Key;
        public TValue Value;

        public readonly KeyValuePair<TKey, TValue> ToKeyValuePair() => new(Key, Value);
    }
}
