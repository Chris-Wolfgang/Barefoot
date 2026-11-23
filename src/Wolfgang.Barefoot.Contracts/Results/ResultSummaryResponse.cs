namespace Wolfgang.Barefoot.Contracts.Results;

/// <summary>
/// The strip above the results grid: what one run found for one search, in a few numbers
/// (DESIGN §12, Results).
/// </summary>
/// <param name="MatchCount">The number of room options that pass the search's digest filters.</param>
/// <param name="FoundCount">The number of room options the run found for the search.</param>
/// <param name="CheapestRoomAmount">
/// The lowest room price among the matches, or <see langword="null"/> when nothing matched.
/// </param>
/// <param name="CheapestTotal">
/// The match with the lowest total, or <see langword="null"/> when nothing matched.
/// </param>
/// <param name="ChangeAmount">
/// The cheapest total less the previous run's cheapest total: negative is cheaper. It is
/// <see langword="null"/> when there is no previous run to compare with.
/// </param>
/// <param name="PreviousCapturedUtc">
/// The instant of the run the change is measured against, or <see langword="null"/> when there is none.
/// </param>
/// <param name="OfflineResorts">
/// The resorts of the search that were offline on the booking site and were not looked up, by short name.
/// </param>
public sealed record ResultSummaryResponse
(
    int MatchCount,
    int FoundCount,
    decimal? CheapestRoomAmount,
    CheapestTotalResponse? CheapestTotal,
    decimal? ChangeAmount,
    DateTimeOffset? PreviousCapturedUtc,
    IReadOnlyList<string> OfflineResorts
);
