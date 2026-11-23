namespace Wolfgang.Barefoot.Contracts.Dashboard;

/// <summary>
/// The digest most recently sent to the caller: the Dashboard's Latest digest card (DESIGN §12).
/// </summary>
/// <param name="DigestId">The digest's id, which My digests opens.</param>
/// <param name="SearchRunId">The id of the search's part of the run, which the Results screen takes.</param>
/// <param name="SearchName">The name of the search the digest reports on.</param>
/// <param name="SentUtc">When the digest was sent.</param>
/// <param name="MatchCount">How many rooms matched the search's filters.</param>
/// <param name="Cheapest">The cheapest total among them, or <see langword="null"/> when none matched.</param>
/// <param name="ResortCount">How many resorts they are at.</param>
public sealed record LatestDigestResponse
(
    Guid DigestId,
    Guid SearchRunId,
    string SearchName,
    DateTimeOffset SentUtc,
    int MatchCount,
    decimal? Cheapest,
    int ResortCount
);
