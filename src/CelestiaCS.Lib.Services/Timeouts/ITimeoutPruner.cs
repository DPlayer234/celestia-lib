using System.Collections.Concurrent;
using System.Threading.Tasks;
using NodaTime;

namespace CelestiaCS.Lib.Timeouts;

public interface ITimeoutPruner
{
    /// <summary>
    /// Gets a dummy <see cref="ITimeoutPruner"/> that does nothing.
    /// </summary>
    static ITimeoutPruner Null => NullTimeoutPruner.Instance;

    Task StartPruningAsync<TKey>(ConcurrentDictionary<TKey, Instant> timeouts, string logLabel) where TKey : notnull;
    int PruneOnce<TKey>(ConcurrentDictionary<TKey, Instant> timeouts, string logLabel) where TKey : notnull;
}
