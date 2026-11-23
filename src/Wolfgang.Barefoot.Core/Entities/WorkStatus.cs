namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// Where one piece of a run's work stands: a room lookup or a flight set. A piece a worker holds a
/// lease on is still <see cref="Pending"/>; the lease says who has it.
/// </summary>
public enum WorkStatus
{
    /// <summary>No status was given. It is what a value nobody set holds, and no row is stored with it.</summary>
    NotSet = 0,

    /// <summary>The work waits to be claimed, or is claimed and has no result yet.</summary>
    Pending = 1,

    /// <summary>The site answered and the result is stored.</summary>
    Succeeded = 2,

    /// <summary>The worker reported an error instead of a result.</summary>
    Failed = 3,
}
