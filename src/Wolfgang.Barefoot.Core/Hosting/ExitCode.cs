namespace Wolfgang.Barefoot.Core.Hosting;

/// <summary>
/// The exit codes of the three command-line hosts, <c>bf-scraper</c>, <c>bf</c> and <c>bf-maint</c>
/// (ADR 0007). One numbering for all three, so no code means two things and a host can report a
/// failure without having reached anything.
/// </summary>
/// <remarks>
/// The scheduled job stops on <see cref="VersionMismatch"/>, <see cref="ApiUnavailable"/>,
/// <see cref="DatabaseUnavailable"/> and <see cref="QuotaExhausted"/>, because nothing later can work,
/// and continues past <see cref="Partial"/> and <see cref="BreakerTripped"/>.
/// <c>docs/EXIT-CODES.md</c> is generated from this enum and <see cref="ExitCodes"/>.
/// </remarks>
public enum ExitCode
{
    /// <summary>Done.</summary>
    Ok = 0,

    /// <summary>The verb failed: a migration failed, or an exception nothing handled.</summary>
    Failed = 1,

    /// <summary>Finished with warnings; some items did not complete.</summary>
    Partial = 2,

    /// <summary>The scraper stopped the run; resume later.</summary>
    BreakerTripped = 3,

    /// <summary>Bad arguments or configuration.</summary>
    Usage = 4,

    /// <summary>The API is too old or too new for this binary.</summary>
    VersionMismatch = 5,

    /// <summary>A downgrade would lose data and <c>--confirm-data-loss</c> was not given.</summary>
    DataLossNotConfirmed = 6,

    /// <summary>The API could not be reached within the retry budget.</summary>
    ApiUnavailable = 7,

    /// <summary>The database could not be reached within the retry budget.</summary>
    DatabaseUnavailable = 8,

    /// <summary>The monthly free allowance is used up.</summary>
    QuotaExhausted = 9,

    /// <summary>
    /// The tool will not act: the database is newer than this build, or the API refused a catalog
    /// snapshot (ADR 0017, ADR 0018).
    /// </summary>
    Refused = 10,

    /// <summary>
    /// The database is behind this build: a migration is not applied. <c>migrate --status</c> reports
    /// it (ADR 0014), and so does a tool that the API answered with that readiness state (ADR 0018).
    /// </summary>
    MissingMigration = 11,
}
