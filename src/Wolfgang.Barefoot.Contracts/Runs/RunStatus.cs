namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// The state of a run, and the outcome of a run for one search (DESIGN §1 and §5).
/// </summary>
public enum RunStatus
{
    /// <summary>Work is still being handed out. At most one run is in progress at a time.</summary>
    InProgress = 0,

    /// <summary>Every lookup was done.</summary>
    Complete = 1,

    /// <summary>Some lookups were not done; the run can be resumed.</summary>
    Incomplete = 2,

    /// <summary>The run did not produce results.</summary>
    Failed = 3,
}
