using Wolfgang.Barefoot.Contracts.Runs;

namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One search's part in one run (DESIGN §8): the check-in window as it resolved on the day, how far
/// the search has come, and how it ended.
/// </summary>
public sealed class MatrixRun
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="Run"/> this is part of.</summary>
    public required Guid RunId { get; set; }



    /// <summary>The <see cref="SearchMatrix"/> that was run.</summary>
    public required Guid SearchMatrixId { get; set; }



    /// <summary>The first check-in the search's window resolved to when the run started.</summary>
    public required DateOnly FirstCheckIn { get; set; }



    /// <summary>The last check-in the search's window resolved to when the run started.</summary>
    public required DateOnly LastCheckIn { get; set; }



    /// <summary>How far the search has come within the run.</summary>
    public required SearchRunStage Stage { get; set; }



    /// <summary>How the run went for this search. It is in progress until the run is finished.</summary>
    public RunStatus Outcome { get; set; }



    /// <summary>
    /// Why the run left the search out, for example because its window had passed, or
    /// <see langword="null"/> when the search was run. A skipped search has no lookups.
    /// </summary>
    public string? SkipReason { get; set; }
}
