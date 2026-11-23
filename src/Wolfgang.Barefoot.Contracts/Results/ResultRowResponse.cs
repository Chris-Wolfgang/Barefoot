namespace Wolfgang.Barefoot.Contracts.Results;

/// <summary>
/// One row of the results grid: one room option of one resort for one check-in date
/// (DESIGN §12, Results).
/// </summary>
/// <param name="Id">The id of the stored room result.</param>
/// <param name="ResortId">The resort.</param>
/// <param name="ResortName">The resort's short name.</param>
/// <param name="CheckIn">The check-in date.</param>
/// <param name="NightCount">The length of the stay in nights.</param>
/// <param name="RoomCategoryCode">
/// The room category code. With the resort and the check-in date it identifies the room across runs.
/// </param>
/// <param name="RoomName">The room name.</param>
/// <param name="Level">The room level, or <see langword="null"/> when it was not read.</param>
/// <param name="View">The room view, or <see langword="null"/> when it was not read.</param>
/// <param name="RoomAmount">The price of the room for the whole stay.</param>
/// <param name="RegularAmount">The price before the sale, or <see langword="null"/> when none was read.</param>
/// <param name="Promotion">The promotion as the site words it, or <see langword="null"/> when none was read.</param>
/// <param name="Flights">
/// One entry per departure airport that has options; empty for a search without flights.
/// </param>
/// <param name="TotalAmount">
/// The room price plus the cheapest airfare of any departure airport, or the room price alone when
/// there are no flights.
/// </param>
/// <param name="ChangeAmount">
/// The total less the previous run's total for the same resort, room and check-in: negative is
/// cheaper, zero unchanged. It is <see langword="null"/> when the previous run had no such room.
/// </param>
/// <param name="IsNew">Whether the previous run had no such room: the row reads "new this run".</param>
/// <param name="MatchesFilters">Whether the option passes the search's digest filters.</param>
public sealed record ResultRowResponse
(
    Guid Id,
    Guid ResortId,
    string ResortName,
    DateOnly CheckIn,
    int NightCount,
    string RoomCategoryCode,
    string RoomName,
    string? Level,
    string? View,
    decimal RoomAmount,
    decimal? RegularAmount,
    string? Promotion,
    IReadOnlyList<ResultFlightsResponse> Flights,
    decimal TotalAmount,
    decimal? ChangeAmount,
    bool IsNew,
    bool MatchesFilters
);
