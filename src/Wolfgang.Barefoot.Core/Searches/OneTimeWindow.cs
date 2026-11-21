namespace Wolfgang.Barefoot.Core.Searches;

/// <summary>
/// A window for one occasion: explicit first and last check-in dates.
/// </summary>
public sealed record OneTimeWindow : CheckInWindow
{
    /// <summary>
    /// Creates a one-time window.
    /// </summary>
    /// <param name="first">The first check-in date.</param>
    /// <param name="last">The last check-in date, on or after <paramref name="first"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="last"/> is before <paramref name="first"/>.</exception>
    public OneTimeWindow(DateOnly first, DateOnly last)
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
}
