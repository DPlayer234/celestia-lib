using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib.Collections.Synced;

public class SyncList<T> : IList<T>, IReadOnlyList<T>
{
    private ImmutableList<T> _inner;

    public SyncList() : this(ImmutableList<T>.Empty) { }

    public SyncList(IEnumerable<T> source) : this(source.ToImmutableList()) { }

    public SyncList(ImmutableList<T> list)
    {
        _inner = list;
    }

    public int Count => _inner.Count;

    public ImmutableList<T> Storage
    {
        get => _inner;
        set => _inner = value;
    }

    public T this[int index]
    {
        get => _inner[index];
        set => Mutate(static (l, a) => l.SetItem(a.index, a.value), (index, value));
    }

    public void Add(T item)
    {
        Mutate(static (l, a) => l.Add(a), item);
    }

    public void AddRange(IEnumerable<T> items)
    {
        Mutate(static (l, a) => l.AddRange(a), items);
    }

    public void Insert(int index, T item)
    {
        Mutate(static (l, a) => l.Insert(a.index, a.item), (index, item));
    }

    public bool Remove(T item)
    {
        return Mutate(static (l, a) => l.Remove(a), item);
    }

    public void RemoveAt(int index)
    {
        Mutate(static (l, a) => l.RemoveAt(a), index);
    }

    public void Clear()
    {
        Mutate(static l => l.Clear());
    }

    public void ReplaceAll(IEnumerable<T> items)
    {
        var list = items.ToImmutableList();
        Mutate(static (l, a) => a, list);
    }

    public int IndexOf(T item)
    {
        return _inner.IndexOf(item);
    }

    public bool Contains(T item)
    {
        return _inner.Contains(item);
    }

    public void Sort(Comparison<T> comparison)
    {
        Mutate(static (l, a) => l.Sort(a), comparison);
    }

    public void Sort(IComparer<T> comparer)
    {
        Mutate(static (l, a) => l.Sort(a), comparer);
    }

    public bool TryAdd(T item)
    {
        return Mutate(static (l, a) => l.Contains(a) ? l : l.Add(a), item);
    }

    public bool TryGetAt(int index, [MaybeNullWhen(false)] out T value)
    {
        var i = _inner;
        if ((uint)index < (uint)i.Count)
        {
            value = i[index];
            return true;
        }

        value = default;
        return false;
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
    private bool Mutate(Func<ImmutableList<T>, ImmutableList<T>> transformer)
    {
        return ImmutableInterlocked.Update(ref _inner, transformer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Mutate<TItem>(Func<ImmutableList<T>, TItem, ImmutableList<T>> transformer, TItem arg)
    {
        return ImmutableInterlocked.Update(ref _inner, transformer, arg);
    }

    bool ICollection<T>.IsReadOnly => false;
    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        private ImmutableList<T>.Enumerator _inner;

        public Enumerator(ImmutableList<T> list)
        {
            _inner = list.GetEnumerator();
        }

        public T Current => _inner.Current;

        public bool MoveNext() => _inner.MoveNext();
        public void Dispose() => _inner.Dispose();
        public void Reset() => _inner.Reset();

        object? IEnumerator.Current => Current;
    }

    public sealed class Builder : ICollection<T>
    {
        private readonly ImmutableList<T>.Builder _inner;

        public Builder() => _inner = ImmutableList.CreateBuilder<T>();
        internal Builder(ImmutableList<T>.Builder builder) => _inner = builder;

        public int Count => _inner.Count;

        public void Add(T item) => _inner.Add(item);
        public bool Remove(T item) => _inner.Remove(item);
        public void Clear() => _inner.Clear();

        public Enumerator GetEnumerator() => new(_inner.ToImmutable());

        public void Apply(SyncList<T> list) => list.Storage = _inner.ToImmutable();
        public SyncList<T> Build() => new(_inner.ToImmutable());

        bool ICollection<T>.IsReadOnly => false;
        bool ICollection<T>.Contains(T item) => _inner.Contains(item);
        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
