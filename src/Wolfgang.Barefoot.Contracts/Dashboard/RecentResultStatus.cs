namespace Wolfgang.Barefoot.Contracts.Dashboard;

/// <summary>
/// How one search fared on one run, as the Dashboard's Recent results list shows it (DESIGN §12).
/// It adds two states to a run's outcome that only a list of searches has: a search waiting its
/// turn, and a search the run left out.
/// </summary>
public enum RecentResultStatus
{
    /// <summary>No status was given. It is what a value nobody set holds, and the API never reports it.</summary>
    NotSet = 0,

    /// <summary>The run is in progress and none of this search's lookups has been done yet.</summary>
    Queued = 1,

    /// <summary>The run is in progress and some of this search's lookups are done.</summary>
    InProgress = 2,

    /// <summary>Every lookup was done.</summary>
    Complete = 3,

    /// <summary>The run finished with some lookups failed.</summary>
    Incomplete = 4,

    /// <summary>The run stopped before the search's lookups were done.</summary>
    Failed = 5,

    /// <summary>The run left the search out, for the reason in the row's note.</summary>
    Skipped = 6,
}
