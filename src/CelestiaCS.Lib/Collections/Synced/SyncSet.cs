using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace CelestiaCS.Lib.Collections.Synced;

public class SyncSet<T> : ISet<T>, IReadOnlySet<T>
{
    private ImmutableHashSet<T> _inner;

    public SyncSet() : this(ImmutableHashSet<T>.Empty) { }

    public SyncSet(IEnumerable<T> source) : this(source.ToImmutableHashSet()) { }

    public SyncSet(ImmutableHashSet<T> set)
    {
        _inner = set;
    }

    public int Count => _inner.Count;

    public ImmutableHashSet<T> Storage
    {
        get => _inner;
        set => _inner = value;
    }

    public bool Add(T item)
    {
        return Mutate(static (l, a) => l.Add(a), item);
    }

    public bool Remove(T item)
    {
        return Mutate(static (l, a) => l.Remove(a), item);
    }

    public bool Contains(T item)
    {
        return _inner.Contains(item);
    }

    public void Clear()
    {
        Mutate(static l => l.Clear());
    }

    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        return _inner.IsProperSubsetOf(other);
    }

    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        return _inner.IsProperSupersetOf(other);
    }

    public bool IsSubsetOf(IEnumerable<T> other)
    {
        return _inner.IsSubsetOf(other);
    }

    public bool IsSupersetOf(IEnumerable<T> other)
    {
        return _inner.IsSupersetOf(other);
    }

    public bool Overlaps(IEnumerable<T> other)
    {
        return _inner.Overlaps(other);
    }

    public bool SetEquals(IEnumerable<T> other)
    {
        return _inner.SetEquals(other);
    }

    public void ExceptWith(IEnumerable<T> other)
    {
        Mutate(static (l, a) => l.Except(a), other);
    }

    public void IntersectWith(IEnumerable<T> other)
    {
        Mutate(static (l, a) => l.Intersect(a), other);
    }

    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        Mutate(static (l, a) => l.SymmetricExcept(a), other);
    }

    public void UnionWith(IEnumerable<T> other)
    {
        Mutate(static (l, a) => l.Union(a), other);
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
    private bool Mutate(Func<ImmutableHashSet<T>, ImmutableHashSet<T>> transformer)
    {
        return ImmutableInterlocked.Update(ref _inner, transformer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Mutate<TItem>(Func<ImmutableHashSet<T>, TItem, ImmutableHashSet<T>> transformer, TItem arg)
    {
        return ImmutableInterlocked.Update(ref _inner, transformer, arg);
    }

    bool ICollection<T>.IsReadOnly => false;
    void ICollection<T>.Add(T item) => Add(item);
    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => ((ICollection<T>)_inner).CopyTo(array, arrayIndex);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        private ImmutableHashSet<T>.Enumerator _inner;

        public Enumerator(ImmutableHashSet<T> list)
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
        private readonly ImmutableHashSet<T>.Builder _inner;

        public Builder() => _inner = ImmutableHashSet.CreateBuilder<T>();
        internal Builder(ImmutableHashSet<T>.Builder builder) => _inner = builder;

        public int Count => _inner.Count;

        public void Add(T item) => _inner.Add(item);
        public bool Remove(T item) => _inner.Remove(item);
        public void Clear() => _inner.Clear();

        public Enumerator GetEnumerator() => new(_inner.ToImmutable());

        public void Apply(SyncSet<T> list) => list.Storage = _inner.ToImmutable();
        public SyncSet<T> Build() => new(_inner.ToImmutable());

        bool ICollection<T>.IsReadOnly => false;
        bool ICollection<T>.Contains(T item) => _inner.Contains(item);
        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => ((ICollection<T>)_inner).CopyTo(array, arrayIndex);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
