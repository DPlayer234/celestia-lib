using NodaTime;

namespace CelestiaCS.Lib.DateIds;

/// <summary>
/// Represents an instant for date IDs.
/// </summary>
/// <param name="dateIds"> The date IDs implementation used. </param>
/// <param name="instant"> The instant's time. </param>
public readonly struct DateIdInstant(IDateIds dateIds, Instant instant)
{
    /// <summary> The instant's time. </summary>
    public Instant Instant => instant;

    /// <summary> Gets the ID for the current day. </summary>
    public long DayId => dateIds.GetDayId(instant);
    /// <summary> Gets the ID for the current week. </summary>
    public long WeekId => dateIds.GetWeekId(instant);
    /// <summary> Gets the ID for the current month. </summary>
    public long MonthId => dateIds.GetMonthId(instant);

    /// <summary> Gets the instant when the next day starts. </summary>
    public DateIdInstant NextDay => dateIds.GetDayOf(DayId + 1);
    /// <summary> Gets the instant when the next week starts. </summary>
    public DateIdInstant NextWeek => dateIds.GetWeekOf(WeekId + 1);
    /// <summary> Gets the instant when the next month starts. </summary>
    public DateIdInstant NextMonth => dateIds.GetMonthOf(MonthId + 1);
}
