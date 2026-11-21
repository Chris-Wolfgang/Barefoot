namespace Wolfgang.Barefoot.Core.Searches;

/// <summary>
/// A window that repeats every year: from one day of the year to another. The year is decided each
/// time the window is resolved.
/// </summary>
/// <param name="First">The first check-in day.</param>
/// <param name="Last">
/// The last check-in day. When it is earlier in the year than <paramref name="First"/> the window runs
/// across the new year, for example 20 December to 5 January.
/// </param>
public sealed record RecurringWindow(MonthDay First, MonthDay Last) : CheckInWindow
{
    /// <summary>Whether the window runs across the new year.</summary>
    public bool WrapsYear => First.IsAfter(Last);
}
