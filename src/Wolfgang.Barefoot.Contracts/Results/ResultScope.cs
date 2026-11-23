namespace Wolfgang.Barefoot.Contracts.Results;

/// <summary>
/// Which of a run's room options the Results screen lists (DESIGN §12, Results).
/// </summary>
public enum ResultScope
{
    /// <summary>Only the options that pass the search's digest filters. The default.</summary>
    MatchingFilters = 0,

    /// <summary>Every option the run found for the search.</summary>
    Everything = 1,
}
