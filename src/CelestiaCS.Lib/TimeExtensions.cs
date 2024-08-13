using System;
using System.Threading;

namespace CelestiaCS.Lib;

/// <summary>
/// Provides extension methods for time related types.
/// </summary>
public static class TimeExtensions
{
    /// <summary>
    /// Determines if the given time span represents a valid timeout.
    /// </summary>
    /// <param name="timeSpan"> The time span. </param>
    /// <returns> If it represents a valid timeout. </returns>
    public static bool IsValidTimeout(this TimeSpan timeSpan)
    {
        return timeSpan == Timeout.InfiniteTimeSpan
            || timeSpan >= TimeSpan.Zero;
    }
}
