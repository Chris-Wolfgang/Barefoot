namespace Wolfgang.Barefoot.Core.Searches;

/// <summary>
/// Turns a check-in window into the dates to look up today (DESIGN §4). A pure function: the caller
/// supplies today's date and the minimum lead, so the result never depends on a clock.
/// </summary>
/// <remarks>
/// <para>
/// The booking site cannot search a check-in closer than a few days away, so no date before
/// <c>today + minimum lead</c> is ever returned.
/// </para>
/// <para>
/// A recurring window resolves to its next occurrence that still has a searchable date. While an
/// occurrence is under way the window starts at the earliest searchable date; once it has passed, the
/// window is next year's. A one-time window never moves: once its last date can no longer be
/// searched it has passed and resolves to nothing.
/// </para>
/// </remarks>
public static class WindowResolver
{
    /// <summary>
    /// The dates <paramref name="window"/> stands for on <paramref name="today"/>.
    /// </summary>
    /// <param name="window">The search's check-in window.</param>
    /// <param name="today">Today's date, in the zone the caller's rule names.</param>
    /// <param name="minimumLeadDays">How many days ahead the earliest searchable check-in is. Zero or more.</param>
    /// <returns>
    /// The resolved window, or <see langword="null"/> when a one-time window has passed.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="window"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumLeadDays"/> is negative.</exception>
    /// <exception cref="NotSupportedException"><paramref name="window"/> is a kind this resolver does not know.</exception>
    public static ResolvedWindow? Resolve
    (
        CheckInWindow window,
        DateOnly today,
        int minimumLeadDays
    )
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumLeadDays);

        var earliest = today.AddDays(minimumLeadDays);

        return window switch
        {
            OneTimeWindow oneTime => ResolveOneTime(oneTime, earliest),
            RecurringWindow recurring => ResolveRecurring(recurring, earliest),
            _ => throw new NotSupportedException($"Unknown kind of check-in window: {window.GetType().Name}."),
        };
    }



    private static ResolvedWindow? ResolveOneTime(OneTimeWindow window, DateOnly earliest)
    {
        if (window.Last < earliest)
        {
            return null;
        }

        return new ResolvedWindow(Later(window.First, earliest), window.Last);
    }



    private static ResolvedWindow ResolveRecurring(RecurringWindow window, DateOnly earliest)
    {
        // The occurrence that began last year matters only for a window that runs across the new
        // year; the one that begins next year is the answer once this year's has passed.
        for (var year = earliest.Year - 1; ; year++)
        {
            var first = window.First.InYear(year);
            var last = window.Last.InYear(window.WrapsYear ? year + 1 : year);

            if (last >= earliest)
            {
                return new ResolvedWindow(Later(first, earliest), last);
            }
        }
    }



    private static DateOnly Later(DateOnly left, DateOnly right)
    {
        return left > right ? left : right;
    }
}
