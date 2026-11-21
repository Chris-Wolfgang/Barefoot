namespace Wolfgang.Barefoot.Contracts.PriceHistory;

/// <summary>
/// Which price history is wanted: one room of one resort on one check-in date, as one search captured
/// it (DESIGN §7: the identity of a price over time is resort, check-in date and room category).
/// </summary>
/// <param name="SearchId">The search whose stay length and occupancy the prices are for.</param>
/// <param name="ResortId">The resort.</param>
/// <param name="RoomCategoryId">The room category.</param>
/// <param name="CheckIn">The check-in date.</param>
/// <param name="DepartureAirportId">
/// The departure airport the airfare is from, or <see langword="null"/> for the room alone.
/// </param>
/// <param name="From">
/// Only captures on or after this date, or <see langword="null"/> for every capture.
/// </param>
/// <param name="Scope">Whose captures to use.</param>
public sealed record PriceHistoryQuery
(
    Guid SearchId,
    Guid ResortId,
    Guid RoomCategoryId,
    DateOnly CheckIn,
    Guid? DepartureAirportId = null,
    DateOnly? From = null,
    PriceHistoryScope Scope = PriceHistoryScope.Mine
);
