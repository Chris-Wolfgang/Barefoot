namespace Wolfgang.Barefoot.Core.Hosting;

/// <summary>
/// What one exit code means, for a person reading a scheduled task's history.
/// </summary>
/// <param name="Code">The exit code.</param>
/// <param name="Meaning">One sentence saying what happened.</param>
/// <param name="UsedBy">The binaries that can exit with the code, in the order they are documented.</param>
/// <param name="StopsTheJob">
/// Whether the scheduled job stops when a step exits with the code, because no later step can work.
/// </param>
public sealed record ExitCodeInfo
(
    ExitCode Code,
    string Meaning,
    IReadOnlyList<string> UsedBy,
    bool StopsTheJob
);
