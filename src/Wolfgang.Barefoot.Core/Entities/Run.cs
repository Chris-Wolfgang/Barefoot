using Wolfgang.Barefoot.Contracts.Runs;

namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One pass over every active search (DESIGN §5). At most one run is in progress at a time; a run
/// knows all its work when it is created.
/// </summary>
public sealed class Run
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>What started the run, as the caller named it (a schedule, a person, a resume).</summary>
    public required string Trigger { get; set; }



    /// <summary>Where the run stands. A run is created in progress.</summary>
    public RunStatus Status { get; set; }



    /// <summary>When the run was created.</summary>
    public DateTimeOffset StartedUtc { get; set; }



    /// <summary>When the run was finished, or <see langword="null"/> while it is not.</summary>
    public DateTimeOffset? FinishedUtc { get; set; }



    /// <summary>
    /// Why the worker stopped early, or <see langword="null"/> when it did not. The rest of the work
    /// waits for a resume.
    /// </summary>
    public string? AbortReason { get; set; }
}
