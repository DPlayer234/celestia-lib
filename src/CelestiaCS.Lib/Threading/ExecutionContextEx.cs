using System;
using System.Threading;

namespace CelestiaCS.Lib.Threading;

/// <summary>
/// Provides additional methods for handling the execution context.
/// </summary>
public static class ExecutionContextEx
{
    /// <summary> Captures the current <see cref="ExecutionContext"/> and restores it when the returned object is disposed. </summary>
    /// <remarks> This allows for temporary modifications to <see cref="AsyncLocal{T}"/> values that can be efficiently discarded as needed. </remarks>
    /// <returns> A scope that can be disposed to restore the original context. </returns>
    public static RestoreScope CaptureAndRestore()
    {
        return RestoreScope.Capture();
    }

    public readonly struct RestoreScope : IDisposable
    {
        // Ideally, this would be supported even if flow is suppressed, but I neither want to restore the flow
        // temporarily nor call into internals, so we just live with this.
        // If necessary, the caller can just restore the flow anyways or, if in async code, yield once.

        private readonly ExecutionContext _ctx;

        private RestoreScope(ExecutionContext ctx) => _ctx = ctx;

        public void Dispose() => ExecutionContext.Restore(_ctx);

        internal static RestoreScope Capture()
        {
            var ctx = ExecutionContext.Capture();
            if (ctx is null)
                ThrowHelper.InvalidOperation($"{nameof(CaptureAndRestore)} is not supported if flow is suppressed.");

            return new RestoreScope(ctx);
        }
    }
}
