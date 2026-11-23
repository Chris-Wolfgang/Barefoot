namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// How far one search has come within a run (DESIGN §8). The stages are passed in order.
/// </summary>
public enum MatrixRunStage
{
    /// <summary>No stage was given. It is what a value nobody set holds, and no row is stored with it.</summary>
    NotSet = 0,

    /// <summary>The search's lookups are planned and wait to be claimed.</summary>
    Exploded = 1,

    /// <summary>Every lookup the search wants has a result or an error.</summary>
    Downloaded = 2,

    /// <summary>The search's digest is built and stored.</summary>
    Aggregated = 3,

    /// <summary>The digest has been sent.</summary>
    Emailed = 4,
}
