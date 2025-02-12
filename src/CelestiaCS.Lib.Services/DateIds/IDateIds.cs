using NodaTime;

namespace CelestiaCS.Lib.DateIds;

/// <summary>
/// Defines an interface to access the current time and IDs for specific time intervals.
/// </summary>
public interface IDateIds
{
    /// <summary> Gets the current instant. </summary>
    sealed DateIdInstant Now => new DateIdInstant(this, NowInstant);

    /// <summary> Gets the instant at the beginning of the day for a day ID. </summary>
    /// <param name="dayId"> The day ID. </param>
    /// <returns> The associated instant. </returns>
    sealed DateIdInstant GetDayOf(long dayId) => new DateIdInstant(this, GetDayOfInstant(dayId));

    /// <summary> Gets the instant at the beginning of the week for a week ID. </summary>
    /// <param name="weekId"> The week ID. </param>
    /// <returns> The associated instant. </returns>
    sealed DateIdInstant GetWeekOf(long weekId) => new DateIdInstant(this, GetWeekOfInstant(weekId));

    /// <summary> Gets the instant at the beginning of the month for a month ID. </summary>
    /// <param name="monthId"> The month ID. </param>
    /// <returns> The associated instant. </returns>
    sealed DateIdInstant GetMonthOf(long monthId) => new DateIdInstant(this, GetMonthOfInstant(monthId));

    /// <summary> Creates a new instant from the provided time. </summary>
    /// <param name="instant"> The time's instant. </param>
    /// <returns> The newly created instant. </returns>
    sealed DateIdInstant CreateInstant(Instant instant) => new DateIdInstant(this, instant);

    // The methods below provide the underlying implementation for the sealed methods above.
    // The public API is a somewhat non-mirrored (DateTime to ID, but ID to DateIdInstant),
    // which is fine since DateIdInstant to ID is supported more directly and it provides slightly more info.
    //
    // The reason that the `*Instant` members are protected is so to reduce the consumer surface area.
    // It doesn't impact how implementations are written.

    /// <summary> Gets the current time. </summary>
    protected Instant NowInstant { get; }

    /// <summary> Gets the ID for the specified day. </summary>
    /// <param name="instant"> The time to get the ID for. </param>
    /// <returns> The day ID. </returns>
    long GetDayId(Instant instant);

    /// <summary> Gets the ID for the specified week. </summary>
    /// <param name="instant"> The time to get the ID for. </param>
    /// <returns> The week ID. </returns>
    long GetWeekId(Instant instant);

    /// <summary> Gets the ID for the specified month. </summary>
    /// <param name="instant"> The time to get the ID for. </param>
    /// <returns> The month ID. </returns>
    long GetMonthId(Instant instant);

    /// <summary> Gets the time at the beginning of the day for a day ID. </summary>
    /// <param name="dayId"> The day ID. </param>
    /// <returns> The associated time. </returns>
    protected Instant GetDayOfInstant(long dayId);

    /// <summary> Gets the time at the beginning of the week for a week ID. </summary>
    /// <param name="weekId"> The week ID. </param>
    /// <returns> The associated time. </returns>
    protected Instant GetWeekOfInstant(long weekId);

    /// <summary> Gets the time at the beginning of the month for a month ID. </summary>
    /// <param name="monthId"> The month ID. </param>
    /// <returns> The associated time. </returns>
    protected Instant GetMonthOfInstant(long monthId);
}
