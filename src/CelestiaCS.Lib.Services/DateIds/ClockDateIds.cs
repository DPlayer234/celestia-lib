using System;
using NodaTime;

namespace CelestiaCS.Lib.DateIds;

/// <summary>
/// A default implementation of <see cref="IDateIds"/> based on a <see cref="IClock"/>.
/// </summary>
public class ClockDateIds : IDateIds
{
    // Unix epoch:
    private const long MonthOffsetInYears = 1970;
    private const long WeekOffsetInSeconds = 4 * NodaConstants.SecondsPerDay;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClockDateIds"/> class.
    /// </summary>
    /// <param name="clock"> The clock to use. </param>
    public ClockDateIds(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        Clock = clock;
    }

    /// <summary> Gets an instance based on the system clock. </summary>
    public static ClockDateIds System => SystemClockImpl.instance;

    /// <summary> The clock used. </summary>
    public IClock Clock { get; }

    /// <inheritdoc/>
    public virtual Instant NowInstant => Clock.GetCurrentInstant();

    /// <inheritdoc/>
    public long GetDayId(Instant instant) => instant.ToUnixTimeSeconds() / NodaConstants.SecondsPerDay;
    /// <inheritdoc/>
    public long GetWeekId(Instant instant) => (instant.ToUnixTimeSeconds() - WeekOffsetInSeconds) / NodaConstants.SecondsPerWeek;
    /// <inheritdoc/>
    public long GetMonthId(Instant instant)
    {
        var date = instant.InUtc();
        return date.Month - 1 + (date.Year - MonthOffsetInYears) * 12;
    }

    /// <inheritdoc/>
    public Instant GetDayOfInstant(long dayId) => Instant.FromUnixTimeSeconds(dayId * NodaConstants.SecondsPerDay);
    /// <inheritdoc/>
    public Instant GetWeekOfInstant(long weekId) => Instant.FromUnixTimeSeconds(weekId * NodaConstants.SecondsPerWeek + WeekOffsetInSeconds);
    /// <inheritdoc/>
    public Instant GetMonthOfInstant(long monthId)
    {
        int month = (int)(monthId % 12);
        int year = (int)(monthId / 12 + MonthOffsetInYears);
        return Instant.FromUtc(year, month + 1, 1, 0, 0);
    }

    #region Delegate to DIMs

    // You can't usually call DIMs on interfaces when the variable isn't
    // statically typed as the interface. So here we provide them explicitly.
    private IDateIds This => this;

    /// <inheritdoc cref="IDateIds.Now"/>
    public DateIdInstant Now => This.Now;

    /// <inheritdoc cref="IDateIds.GetDayOf(long)"/>
    public DateIdInstant GetDayOf(long dayId) => This.GetDayOf(dayId);

    /// <inheritdoc cref="IDateIds.GetWeekOf(long)"/>
    public DateIdInstant GetWeekOf(long weekId) => This.GetWeekOf(weekId);

    /// <inheritdoc cref="IDateIds.GetMonthOf(long)"/>
    public DateIdInstant GetMonthOf(long monthId) => This.GetMonthOf(monthId);

    /// <inheritdoc cref="IDateIds.CreateInstant(Instant)"/>
    public DateIdInstant CreateInstant(Instant instant) => This.CreateInstant(instant);

    #endregion

    private sealed class SystemClockImpl() : ClockDateIds(SystemClock.Instance)
    {
        internal static readonly SystemClockImpl instance = new();
        public override Instant NowInstant => SystemClock.Instance.GetCurrentInstant();
    }
}
