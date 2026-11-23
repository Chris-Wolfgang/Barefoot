namespace Wolfgang.Barefoot.Contracts.Dashboard;

/// <summary>
/// One search on one run: a row of the Dashboard's Recent results list (DESIGN §12). Clicking the
/// row opens that run's results.
/// </summary>
/// <param name="SearchId">The search's id.</param>
/// <param name="SearchName">The search's name.</param>
/// <param name="Nights">How many nights the search's stay is.</param>
/// <param name="Adults">How many adults travel.</param>
/// <param name="Children">How many children travel.</param>
/// <param name="Infants">How many infants travel.</param>
/// <param name="IncludeFlights">Whether the search looks up flights.</param>
/// <param name="SearchRunId">
/// The id of the search's part of the run, which the Results screen takes, or <see langword="null"/>
/// when the run left the search out.
/// </param>
/// <param name="RunStartedUtc">When the run started.</param>
/// <param name="DurationMinutes">
/// How long the search's part of the run took, or <see langword="null"/> while it is not over or when
/// the search was left out.
/// </param>
/// <param name="Status">How the search fared.</param>
/// <param name="LookupsDone">How many of the search's lookups have a result or an error.</param>
/// <param name="LookupsTotal">How many lookups the search wanted.</param>
/// <param name="LookupsFailed">How many of them failed.</param>
/// <param name="Note">
/// Why the search failed or was left out, in a few words ("site unavailable", "window passed"), or
/// <see langword="null"/> when there is nothing to add.
/// </param>
/// <param name="Cheapest">
/// The cheapest total found, or <see langword="null"/> when nothing was found. While the run is in
/// progress it is the cheapest so far.
/// </param>
/// <param name="ChangeSincePrevious">
/// The cheapest total less the previous run's: negative for a drop. It is <see langword="null"/>
/// when either run has no total, and while the run is in progress.
/// </param>
/// <param name="DigestSentUtc">When the search's digest was sent to the caller, or <see langword="null"/> when it was not.</param>
public sealed record RecentResultResponse
(
    Guid SearchId,
    string SearchName,
    int Nights,
    int Adults,
    int Children,
    int Infants,
    bool IncludeFlights,
    Guid? SearchRunId,
    DateTimeOffset RunStartedUtc,
    int? DurationMinutes,
    RecentResultStatus Status,
    int LookupsDone,
    int LookupsTotal,
    int LookupsFailed,
    string? Note,
    decimal? Cheapest,
    decimal? ChangeSincePrevious,
    DateTimeOffset? DigestSentUtc
);
