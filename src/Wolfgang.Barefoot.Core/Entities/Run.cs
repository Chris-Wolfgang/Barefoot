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



    /// <summary>
    /// When the run was last resumed, or <see langword="null"/> when it never was.
    /// </summary>
    public DateTimeOffset? ResumedUtc { get; set; }



    /// <summary>
    /// How many seconds the run had worked before it was last resumed: every earlier stretch added
    /// up, the waits between them left out. It is zero for a run that was never resumed.
    /// </summary>
    public int WorkedSecondsBeforeResume { get; set; }



    /// <summary>
    /// The worker that last claimed work in the run, as it named itself, or <see langword="null"/>
    /// when nothing has been claimed yet.
    /// </summary>
    public string? WorkerName { get; set; }



    /// <summary>
    /// The version of that worker's build, or <see langword="null"/> when it did not say.
    /// </summary>
    public string? WorkerBuildVersion { get; set; }



    /// <summary>
    /// When work in the run was last claimed, or <see langword="null"/> when none has been. Unlike
    /// a lookup's lease, it is kept after the work moves on.
    /// </summary>
    public DateTimeOffset? LastClaimUtc { get; set; }
}
