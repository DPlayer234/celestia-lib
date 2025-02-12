using NodaTime;

namespace CelestiaCS.Lib;

/// <summary>
/// Provides constants to work with time.
/// </summary>
public static class TimeConstants
{
    /// <summary> An <see cref="Interval"/> that covers all of time. </summary>
    public static Interval AlwaysInterval { get; } = new Interval(null, null);
}
