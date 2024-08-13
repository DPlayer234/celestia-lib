using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CelestiaCS.Lib;

/// <summary>
/// Provides "null" or no-op interface implementation singletons.
/// </summary>
public static class NullImpl
{
    /// <summary>
    /// Gets a disposable that refers to no resources.
    /// </summary>
    public static IDisposable Disposable => DisposableImpl.Instance;

    /// <summary>
    /// Gets an async disposable that refers to no resources.
    /// </summary>
    public static IAsyncDisposable AsyncDisposable => AsyncDisposableImpl.Instance;

    /// <summary>
    /// Gets an empty, reusable enumerator.
    /// </summary>
    public static IEnumerator Enumerator => EnumeratorImpl.Instance;

    /// <summary>
    /// Gets an empty, generic, reusable enumerator.
    /// </summary>
    public static IEnumerator<T> EnumeratorOf<T>() => EnumeratorImpl<T>.Instance;

    private class DisposableImpl : IDisposable
    {
        public static readonly DisposableImpl Instance = new();
        public void Dispose() { }
    }

    private class AsyncDisposableImpl : IAsyncDisposable
    {
        public static readonly AsyncDisposableImpl Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private class EnumeratorImpl : DisposableImpl, IEnumerator
    {
        public static new readonly EnumeratorImpl Instance = new();
        public object? Current => throw new InvalidOperationException("Enumerator is empty.");
        public bool MoveNext() => false;
        public void Reset() { }
    }

    private class EnumeratorImpl<T> : EnumeratorImpl, IEnumerator<T>
    {
        public static new readonly EnumeratorImpl<T> Instance = new();
        public new T Current => (T)base.Current!;
    }
}
