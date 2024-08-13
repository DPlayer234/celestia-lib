using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CelestiaCS.Lib.Threading.CompilerServices;

/// <summary>
/// An awaitable/awaiter for a <see cref="CancellationToken"/>. This type is only intended to be used explicitly by the compiler.
/// </summary>
/// <remarks>
/// Use <see cref="CancellationEx.WaitForCancellationAsync(CancellationToken)"/>.
/// The <see langword="default"/> of this type is not supported.
/// </remarks>
public readonly struct CancellationTokenAwaiter : ICriticalNotifyCompletion
{
    private readonly CancellationToken _token;

    internal CancellationTokenAwaiter(CancellationToken token) => _token = token;

    public bool IsCompleted => _token.IsCancellationRequested;

    public void GetResult() => Debug.Assert(IsCompleted);
    public void OnCompleted(Action continuation) => _token.Register(continuation);
    public void UnsafeOnCompleted(Action continuation) => _token.UnsafeRegister(o => ((Action)o!).Invoke(), continuation);

    public CancellationTokenAwaiter GetAwaiter() => this;
}
