using Wolfgang.Barefoot.Contracts;
using Wolfgang.Barefoot.Contracts.Results;

namespace Wolfgang.Barefoot.Core.Results;

/// <summary>
/// Reads what a run found for a search (DESIGN §12, Results). The caller sees only the results of
/// searches visible to them: their own, or every search for an Admin (ADR 0004 §4). Anything else is
/// reported as not found.
/// </summary>
/// <remarks>
/// Nothing here changes anything. Every room option a run found is kept; the search's digest filters
/// only decide which of them <see cref="ResultScope.MatchingFilters"/> lists.
/// </remarks>
public interface IResultService
{
    /// <summary>
    /// One page of the runs of a search, the latest first: the Results screen's run picker.
    /// </summary>
    /// <param name="searchId">The search's id.</param>
    /// <param name="page">Which page.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<Page<ResultRunResponse>> ListRunsAsync
    (
        Guid searchId,
        PageRequest page,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// The summary of one run of a search: the counts, the cheapest prices and the change since the
    /// previous run.
    /// </summary>
    /// <param name="searchRunId">The id of the search's part of the run.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<ResultSummaryResponse> GetSummaryAsync(Guid searchRunId, CancellationToken cancellationToken);



    /// <summary>
    /// One page of the room options one run found for a search. The default order is the cheapest
    /// total first.
    /// </summary>
    /// <param name="searchRunId">The id of the search's part of the run.</param>
    /// <param name="scope">Whether to list only the options that pass the digest filters, or all of them.</param>
    /// <param name="find">
    /// Text an option's resort or room name must contain, or <see langword="null"/> to list them all.
    /// </param>
    /// <param name="page">Which page, and in which order.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<Page<ResultRowResponse>> ListAsync
    (
        Guid searchRunId,
        ResultScope scope,
        string? find,
        PageRequest page,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// One page of the round trips of a flight set, the cheapest first. The page size comes from the
    /// setting <c>results.flight_options_page_size</c> when the request names none (DESIGN §6).
    /// </summary>
    /// <param name="flightSetId">The flight set's id, from a row's <see cref="ResultFlightsResponse.FlightSetId"/>.</param>
    /// <param name="page">Which page.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<Page<FlightOptionResponse>> ListFlightOptionsAsync
    (
        Guid flightSetId,
        PageRequest page,
        CancellationToken cancellationToken
    );
}
