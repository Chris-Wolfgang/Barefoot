namespace Wolfgang.Barefoot.Contracts.PriceHistory;

/// <summary>
/// What the Price history screen's room and check-in pickers offer for one resort of one search:
/// only what was actually captured.
/// </summary>
/// <param name="Rooms">The rooms that have at least one capture, by name.</param>
/// <param name="CheckIns">The check-in dates that have at least one capture, earliest first.</param>
public sealed record PriceHistoryOptionsResponse
(
    IReadOnlyList<PriceHistoryRoomOption> Rooms,
    IReadOnlyList<DateOnly> CheckIns
);
