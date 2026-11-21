using Microsoft.Extensions.Time.Testing;

namespace Wolfgang.Barefoot.TestKit;

/// <summary>
/// The one source of a clock for tests. A test asks here for a <see cref="FakeTimeProvider"/>,
/// passes it to the code under test and moves it with <see cref="FakeTimeProvider.Advance(TimeSpan)"/>,
/// so no test depends on the day or the time zone it happens to run in.
/// </summary>
/// <remarks>
/// Every clock made here uses UTC as its local time zone, whatever the machine is set to.
/// </remarks>
public static class TestClock
{
    /// <summary>
    /// Where a clock starts when the test does not care: Wednesday 14 January 2026, 12:00 UTC.
    /// Midweek and midday, so neither a date boundary nor a weekend is one tick away.
    /// </summary>
    public static readonly DateTimeOffset DefaultStart = new(2026, 1, 14, 12, 0, 0, TimeSpan.Zero);



    /// <summary>
    /// A clock that starts at <see cref="DefaultStart"/>.
    /// </summary>
    public static FakeTimeProvider Create()
    {
        return new FakeTimeProvider(DefaultStart);
    }



    /// <summary>
    /// A clock that starts at <paramref name="start"/>.
    /// </summary>
    /// <param name="start">The instant the clock first reports. Any offset is accepted; the clock reports it in UTC.</param>
    public static FakeTimeProvider At(DateTimeOffset start)
    {
        return new FakeTimeProvider(start.ToUniversalTime());
    }



    /// <summary>
    /// A clock that starts at the given UTC date and time of day.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month, 1 to 12.</param>
    /// <param name="day">The day of the month.</param>
    /// <param name="hour">The hour, 0 to 23. Midnight when omitted.</param>
    /// <param name="minute">The minute, 0 to 59.</param>
    /// <exception cref="ArgumentOutOfRangeException">The values do not name a real date and time.</exception>
    public static FakeTimeProvider AtUtc
    (
        int year,
        int month,
        int day,
        int hour = 0,
        int minute = 0
    )
    {
        return new FakeTimeProvider(new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero));
    }
}
