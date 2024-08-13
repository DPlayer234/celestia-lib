using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace CelestiaCS.Lib.Threading.Internal;

internal sealed class CovariantValueTaskSource<T> : IValueTaskSource, IValueTaskSource<T>
{
    private ValueTaskAwaiter<T> _awaiter;
    private ManualResetValueTaskSourceCore<T> _core;

    private readonly Action _onSourceTaskCompleted;

    private CovariantValueTaskSource()
    {
        _onSourceTaskCompleted = OnSourceTaskCompleted;
    }

    public short Token => _core.Version;

    void IValueTaskSource.GetResult(short token)
    {
        try
        {
            _ = _core.GetResult(token);
        }
        finally
        {
            ReturnToCache();
        }
    }

    public T GetResult(short token)
    {
        try
        {
            return _core.GetResult(token);
        }
        finally
        {
            ReturnToCache();
        }
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return _core.GetStatus(token);
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _core.OnCompleted(continuation, state, token, flags);
    }

    public static CovariantValueTaskSource<T> RentFor(in ValueTask<T> task)
    {
        var value = TlsPerCoreCache<CovariantValueTaskSource<T>>.Rent() ?? new();
        value.Setup(task);
        return value;
    }

    private void ReturnToCache()
    {
        _awaiter = default;
        _core.Reset();

        TlsPerCoreCache<CovariantValueTaskSource<T>>.Return(this);
    }

    private void Setup(in ValueTask<T> task)
    {
        _awaiter = task.GetAwaiter();
        _awaiter.OnCompleted(_onSourceTaskCompleted);
    }

    private void OnSourceTaskCompleted()
    {
        Debug.Assert(_awaiter.IsCompleted);

        try
        {
            T result = _awaiter.GetResult();
            _core.SetResult(result);
        }
        catch (Exception ex)
        {
            _core.SetException(ex);
        }
    }
}
