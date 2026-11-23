using Wolfgang.Barefoot.Contracts.PriceHistory;

namespace Wolfgang.Barefoot.Core.PriceHistory;

/// <summary>
/// Reads how a room's price has moved from run to run (DESIGN §12, Price history). An Editor's
/// history is built only from their own searches; an Admin may ask for every user's captures.
/// A search that is not visible to the caller is reported as not found.
/// </summary>
public interface IPriceHistoryService
{
    /// <summary>
    /// The rooms and check-in dates that have captures for one resort of one search: what the
    /// screen's pickers offer.
    /// </summary>
    /// <param name="searchId">The search's id.</param>
    /// <param name="resortId">The resort's id. It must be one of the search's resorts.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<PriceHistoryOptionsResponse> GetOptionsAsync
    (
        Guid searchId,
        Guid resortId,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// The captures of one room on one check-in date, oldest first.
    /// </summary>
    /// <param name="query">
    /// Which room, date and departure airport, over which range. Asking for
    /// <see cref="PriceHistoryScope.AllUsers"/> without being an Admin is refused.
    /// </param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<PriceHistoryResponse> GetAsync(PriceHistoryQuery query, CancellationToken cancellationToken);
}
