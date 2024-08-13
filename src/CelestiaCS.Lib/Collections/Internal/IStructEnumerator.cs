using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CelestiaCS.Lib.Collections.Internal;

internal interface IStructEnumerator<T>
{
    T Current { get; }

    bool MoveNext();
}

internal sealed class StructEnumerator<T, TEnumerator> : IEnumerator<T>
    where TEnumerator : struct, IStructEnumerator<T>
{
    [SuppressMessage("Style", "IDE0044", Justification = "Mutable structs expected.")]
    private TEnumerator _inner;

    public StructEnumerator(in TEnumerator inner) => _inner = inner;

    T IEnumerator<T>.Current => _inner.Current;
    object? IEnumerator.Current => _inner.Current;

    bool IEnumerator.MoveNext() => _inner.MoveNext();
    void IEnumerator.Reset() => throw new NotSupportedException();
    void IDisposable.Dispose() { }
}

internal struct InverseStructEnumerator<T, TEnumerator> : IStructEnumerator<T>
    where TEnumerator : IEnumerator<T>
{
    [SuppressMessage("Style", "IDE0044", Justification = "Mutable structs expected.")]
    private TEnumerator _inner;

    public InverseStructEnumerator(TEnumerator inner) => _inner = inner;

    public T Current => _inner.Current;
    public bool MoveNext() => _inner.MoveNext();
}

internal static class StructEnumerator
{
    internal static IEnumerator<T> WrapArray<T>(T[] array)
    {
        return new StructEnumerator<T, ArrayEnumerator<T>>(new ArrayEnumerator<T>(array));
    }
}
