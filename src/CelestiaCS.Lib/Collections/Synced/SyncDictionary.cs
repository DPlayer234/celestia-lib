using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib.Collections.Synced;

public class SyncDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private ImmutableDictionary<TKey, TValue> _inner;

    public SyncDictionary() : this(ImmutableDictionary<TKey, TValue>.Empty) { }

    public SyncDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source) : this(source.ToImmutableDictionary()) { }

    public SyncDictionary(ImmutableDictionary<TKey, TValue> dictionary)
    {
        _inner = dictionary;
    }

    public int Count => _inner.Count;

    public ImmutableDictionary<TKey, TValue> Storage
    {
        get => _inner;
        set => _inner = value;
    }

    public ICollection<TKey> Keys => new KeyOrValueCollection<TKey>(_inner.Count, _inner.Keys);
    public ICollection<TValue> Values => new KeyOrValueCollection<TValue>(_inner.Count, _inner.Values);

    public TValue this[TKey key]
    {
        get => _inner[key];
        set => Mutate(static (l, a) => l.SetItem(a.key, a.value), (key, value));
    }

    public bool ContainsKey(TKey key)
    {
        return _inner.ContainsKey(key);
    }

    public bool TryAdd(TKey key, TValue value)
    {
        return ImmutableInterlocked.TryAdd(ref _inner, key, value);
    }

    public bool TryRemove(TKey key)
    {
        return TryRemove(key, out _);
    }

    public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return ImmutableInterlocked.TryRemove(ref _inner, key, out value);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return _inner.TryGetValue(key, out value);
    }

    public void Clear()
    {
        Mutate(static l => l.Clear());
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(_inner);
    }

    public Builder ToBuilder()
    {
        return new Builder(_inner.ToBuilder());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Mutate(Func<ImmutableDictionary<TKey, TValue>, ImmutableDictionary<TKey, TValue>> transformer)
    {
        return ImmutableInterlocked.Update(ref _inner, transformer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Mutate<TItem>(Func<ImmutableDictionary<TKey, TValue>, TItem, ImmutableDictionary<TKey, TValue>> transformer, TItem arg)
    {
        return ImmutableInterlocked.Update(ref _inner, transformer, arg);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
    {
        Mutate(static (l, a) => l.Add(a.key, a.value), (key, value));
    }

    bool IDictionary<TKey, TValue>.Remove(TKey key)
    {
        return Mutate(static (l, a) => l.Remove(a), key);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        Mutate(static (l, a) => l.Add(a.Key, a.Value), item);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        return _inner.Contains(item);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<TKey, TValue>>)_inner).CopyTo(array, arrayIndex);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        return Mutate(static (l, a) => l.TryGetValue(a.Key, out var v) && EqualityComparer<TValue>.Default.Equals(v, a.Value) ? l.Remove(a.Key) : l, item);
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private ImmutableDictionary<TKey, TValue>.Enumerator _inner;

        public Enumerator(ImmutableDictionary<TKey, TValue> list)
        {
            _inner = list.GetEnumerator();
        }

        public KeyValuePair<TKey, TValue> Current => _inner.Current;

        public bool MoveNext() => _inner.MoveNext();
        public void Dispose() => _inner.Dispose();
        public void Reset() => _inner.Reset();

        object? IEnumerator.Current => Current;
    }

    public sealed class Builder : IDictionary<TKey, TValue>
    {
        private readonly ImmutableDictionary<TKey, TValue>.Builder _inner;

        public Builder() => _inner = ImmutableDictionary.CreateBuilder<TKey, TValue>();
        internal Builder(ImmutableDictionary<TKey, TValue>.Builder builder) => _inner = builder;

        public int Count => _inner.Count;

        public void Add(TKey key, TValue value) => _inner.Add(key, value);
        public bool Remove(TKey key) => _inner.Remove(key);
        public void Clear() => _inner.Clear();

        public Enumerator GetEnumerator() => new(_inner.ToImmutable());

        public void Apply(SyncDictionary<TKey, TValue> list) => list.Storage = _inner.ToImmutable();
        public SyncDictionary<TKey, TValue> Build() => new(_inner.ToImmutable());

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => new KeyOrValueCollection<TKey>(_inner.Count, _inner.ToImmutable().Keys);
        ICollection<TValue> IDictionary<TKey, TValue>.Values => new KeyOrValueCollection<TValue>(_inner.Count, _inner.ToImmutable().Values);
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        TValue IDictionary<TKey, TValue>.this[TKey key] { get => _inner[key]; set => _inner[key] = value; }

        bool IDictionary<TKey, TValue>.ContainsKey(TKey key) => _inner.ContainsKey(key);
        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _inner.TryGetValue(key, out value);

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => _inner.Add(item);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => _inner.Contains(item);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => _inner.Remove(item);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).CopyTo(array, arrayIndex);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class KeyOrValueCollection<THeld> : ICollection<THeld>
    {
        private readonly int _count;
        private readonly IEnumerable<THeld> _values;

        public KeyOrValueCollection(int count, IEnumerable<THeld> values)
        {
            _values = values;
            _count = count;
        }

        int ICollection<THeld>.Count => _count;

        bool ICollection<THeld>.IsReadOnly => true;

        void ICollection<THeld>.Add(THeld item) => throw new NotSupportedException();
        bool ICollection<THeld>.Remove(THeld item) => throw new NotSupportedException();
        void ICollection<THeld>.Clear() => throw new NotSupportedException();

        bool ICollection<THeld>.Contains(THeld item) => _values.Contains(item);

        void ICollection<THeld>.CopyTo(THeld[] array, int arrayIndex)
        {
            foreach (var item in _values)
                array[arrayIndex++] = item;
        }

        IEnumerator<THeld> IEnumerable<THeld>.GetEnumerator() => _values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
    }
}
