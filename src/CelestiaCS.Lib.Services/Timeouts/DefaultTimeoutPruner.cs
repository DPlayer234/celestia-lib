using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CelestiaCS.Lib.Threading;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace CelestiaCS.Lib.Timeouts;

public sealed partial class ClockTimeoutPruner : ITimeoutPruner
{
    private readonly DelayTimer _delay = new();

    private readonly IClock _clock;
    private readonly ILogger _log;

    public ClockTimeoutPruner(IClock clock, ILogger<ClockTimeoutPruner> log)
    {
        _clock = clock;
        _log = log;
    }

    public async Task StartPruningAsync<TKey>(ConcurrentDictionary<TKey, Instant> timeouts, string logLabel)
        where TKey : notnull
    {
        try
        {
            // Prune delays are:
            // - 1 minute / 250 timeouts
            // - At least 5 minutes
            // - First prune at minimum time

            TimeSpan minDelay = TimeSpan.FromMinutes(5);
            TimeSpan delayMult = TimeSpan.FromMinutes(0.004);
            TimeSpan nextDelay = minDelay;

            while (true)
            {
                try
                {
                    // Delay to wait for the next check
                    await _delay.WaitAsync(nextDelay);

                    // Actually perform prune
                    int total = PruneOnce(timeouts, logLabel);

                    // Get new delay from total count
                    TimeSpan adjDelay = total * delayMult;
                    nextDelay = adjDelay > minDelay ? adjDelay : minDelay;
                }
                catch (Exception ex)
                {
                    Log_PruningFailed(ex, logLabel);
                }
            }
        }
        catch (Exception ex)
        {
            Log_PruningStartFailed(ex, logLabel);
        }
    }

    public int PruneOnce<TKey>(ConcurrentDictionary<TKey, Instant> timeouts, string logLabel)
        where TKey : notnull
    {
        Instant now = _clock.GetCurrentInstant();

        int total = 0;
        int pruned = 0;

        foreach (var entry in timeouts)
        {
            // If the cooldown is over, we remove the exact entry
            if (entry.Value < now && timeouts.TryRemove(entry))
            {
                pruned += 1;
            }

            total += 1;
        }

#if !DEBUG
        if (pruned > 0)
        {
#endif
        Log_Pruned(pruned, logLabel);
#if !DEBUG
        }
#endif
        return total;
    }

    #region Logger Messages

    [LoggerMessage(101, LogLevel.Debug, "Pruned {PruneCount} {Label} timeouts.")]
    private partial void Log_Pruned(int pruneCount, string label);

    [LoggerMessage(401, LogLevel.Error, "An error occurred during {Label} timeout pruning.")]
    private partial void Log_PruningFailed(Exception exception, string label);

    [LoggerMessage(501, LogLevel.Critical, "An error occurred *starting* {Label} timeout pruning. Timeout pruning is inactive.")]
    private partial void Log_PruningStartFailed(Exception exception, string label);

    #endregion
}
