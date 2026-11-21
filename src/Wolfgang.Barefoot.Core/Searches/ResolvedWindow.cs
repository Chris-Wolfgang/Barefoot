namespace Wolfgang.Barefoot.Core.Searches;

/// <summary>
/// The actual check-in dates a window stands for on a given day: a first and a last date, both
/// included.
/// </summary>
public readonly record struct ResolvedWindow
{
    /// <summary>
    /// Creates a resolved window.
    /// </summary>
    /// <param name="first">The first check-in date.</param>
    /// <param name="last">The last check-in date, on or after <paramref name="first"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="last"/> is before <paramref name="first"/>.</exception>
    public ResolvedWindow(DateOnly first, DateOnly last)
    {
        if (last < first)
        {
            throw new ArgumentException($"The last check-in date {last:yyyy-MM-dd} is before the first, {first:yyyy-MM-dd}.", nameof(last));
        }

        First = first;
        Last = last;
    }



    /// <summary>The first check-in date.</summary>
    public DateOnly First { get; }



    /// <summary>The last check-in date.</summary>
    public DateOnly Last { get; }



    /// <summary>The number of check-in dates in the window.</summary>
    public int DayCount => Last.DayNumber - First.DayNumber + 1;



    /// <summary>
    /// Every check-in date in the window, in order.
    /// </summary>
    public IEnumerable<DateOnly> Dates()
    {
        for (var date = First; date <= Last; date = date.AddDays(1))
        {
            yield return date;

            if (date == DateOnly.MaxValue)
            {
                yield break;
            }
        }
    }
}
