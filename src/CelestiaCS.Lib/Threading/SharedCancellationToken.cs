using System.Threading;

namespace CelestiaCS.Lib.Threading;

/// <summary>
/// Propagates notifications that an operation was cancelled while also allowing the operation to be canceled directly.
/// </summary>
/// <remarks>
/// This type serves to share a <see cref="CancellationTokenSource"/> with no direct way to reset it while also serving as a <see cref="CancellationToken"/> itself.
/// Much like <see cref="CancellationToken"/>, this type too may represent an uncancellable token.
/// </remarks>
public readonly struct SharedCancellationToken
{
    private readonly CancellationTokenSource? _source;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedCancellationToken"/> struct.
    /// </summary>
    /// <param name="source"> The cancellation token source to use. </param>
    public SharedCancellationToken(CancellationTokenSource? source)
    {
        _source = source;
    }

    /// <summary>
    /// Returns an empty <see cref="SharedCancellationToken"/> value, equivalent to <see cref="CancellationToken.None"/>.
    /// </summary>
    public static SharedCancellationToken None => default;

    /// <summary> Gets whether this token is capable of being in the canceled state. </summary>
    public bool CanBeCanceled => _source != null;
    /// <summary> Gets whether cancellation has been requested for this token. </summary>
    public bool IsCancellationRequested => _source?.IsCancellationRequested ?? false;
    /// <summary> Gets this token as a regular cancellation token. </summary>
    public CancellationToken Token => _source?.Token ?? CancellationToken.None;

    /// <summary>
    /// Communicates a request for cancellation.
    /// </summary>
    public void Cancel() => _source?.Cancel();

    /// <summary>
    /// Implicitly converts a shared token into a regular one.
    /// </summary>
    /// <param name="token"> The token to convert. </param>
    public static implicit operator CancellationToken(SharedCancellationToken token) => token.Token;
}
