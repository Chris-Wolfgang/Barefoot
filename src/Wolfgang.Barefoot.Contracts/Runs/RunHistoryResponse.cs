namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// One past run: a row of the history on the Admin's Runs page (DESIGN §12).
/// </summary>
/// <param name="Id">The run's id.</param>
/// <param name="Trigger">What started the run, as the caller named it.</param>
/// <param name="StartedUtc">When the run was created.</param>
/// <param name="ResumedUtc">When the run was last resumed, or <see langword="null"/> when it never was.</param>
/// <param name="Status">How the run ended.</param>
/// <param name="DurationMinutes">
/// How long the worker was at it, a resume's time added to the first attempt's and the wait between
/// them left out. It is <see langword="null"/> when the run was never finished.
/// </param>
/// <param name="Lookups">The run's room lookups.</param>
/// <param name="FlightSets">The run's flight sets.</param>
/// <param name="DigestsSent">How many of the run's digests were sent.</param>
/// <param name="DigestsFailed">How many could not be delivered.</param>
/// <param name="SecondsPerLookup">
/// The run's time divided by the lookups and flight sets done, or <see langword="null"/> when none
/// was done.
/// </param>
/// <param name="Note">
/// Why the worker stopped early, as it reported it, or <see langword="null"/> when it did not. It is
/// shown under the row.
/// </param>
public sealed record RunHistoryResponse
(
    Guid Id,
    string Trigger,
    DateTimeOffset StartedUtc,
    DateTimeOffset? ResumedUtc,
    RunStatus Status,
    int? DurationMinutes,
    WorkProgress Lookups,
    WorkProgress FlightSets,
    int DigestsSent,
    int DigestsFailed,
    decimal? SecondsPerLookup,
    string? Note
);
