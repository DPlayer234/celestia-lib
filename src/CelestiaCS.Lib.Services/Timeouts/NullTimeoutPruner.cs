using System.Collections.Concurrent;
using System.Threading.Tasks;
using NodaTime;

namespace CelestiaCS.Lib.Timeouts;

/// <summary>
/// Dummy <see cref="ITimeoutPruner"/> that does nothing.
/// </summary>
internal sealed class NullTimeoutPruner : ITimeoutPruner
{
    public static readonly NullTimeoutPruner Instance = new();

    private NullTimeoutPruner() { }

    public int PruneOnce<TKey>(ConcurrentDictionary<TKey, Instant> timeouts, string logLabel) where TKey : notnull
    {
        return 0;
    }

    public Task StartPruningAsync<TKey>(ConcurrentDictionary<TKey, Instant> timeouts, string logLabel) where TKey : notnull
    {
        return Task.CompletedTask;
    }
}
