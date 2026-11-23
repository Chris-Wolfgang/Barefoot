using System.Globalization;
using System.Runtime.InteropServices;
using Wolfgang.Extensions.IComparable;

namespace Wolfgang.Barefoot.Core.Searches;

/// <summary>
/// A day of the year with no year: one end of a check-in window that repeats every year (DESIGN §4).
/// </summary>
/// <remarks>
/// 29 February is a valid value. In a year that has no such day it means 28 February.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct MonthDay
{
    /// <summary>A leap year, used to decide how many days a month can have.</summary>
    private const int LeapYear = 2024;



    /// <summary>
    /// Creates a day of the year.
    /// </summary>
    /// <param name="month">The month, 1 to 12.</param>
    /// <param name="day">The day of the month, 1 to the longest that month can be.</param>
    /// <exception cref="ArgumentOutOfRangeException">The month or the day does not exist.</exception>
    public MonthDay(int month, int day)
    {
        if (!month.IsInRange(1, 12))
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "A month is between 1 and 12.");
        }

        if (!day.IsInRange(1, DateTime.DaysInMonth(LeapYear, month)))
        {
            throw new ArgumentOutOfRangeException(nameof(day), day, $"Month {month} has no day {day}.");
        }

        Month = month;
        Day = day;
    }



    /// <summary>The month, 1 to 12.</summary>
    public int Month { get; }



    /// <summary>The day of the month.</summary>
    public int Day { get; }



    /// <summary>
    /// Whether this day comes after <paramref name="other"/> within one calendar year.
    /// </summary>
    /// <param name="other">The day to compare with.</param>
    public bool IsAfter(MonthDay other)
    {
        return Month > other.Month || (Month == other.Month && Day > other.Day);
    }



    /// <summary>
    /// This day in <paramref name="year"/>. 29 February becomes 28 February in a year without it.
    /// </summary>
    /// <param name="year">The year, 1 to 9999.</param>
    /// <exception cref="ArgumentOutOfRangeException">The year is outside 1 to 9999.</exception>
    public DateOnly InYear(int year)
    {
        return new DateOnly(year, Month, Math.Min(Day, DateTime.DaysInMonth(year, Month)));
    }



    /// <summary>
    /// The value as <c>MM-dd</c>, for example <c>03-15</c>.
    /// </summary>
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Month:00}-{Day:00}");
    }
}
