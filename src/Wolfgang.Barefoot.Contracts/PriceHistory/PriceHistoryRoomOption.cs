namespace Wolfgang.Barefoot.Contracts.PriceHistory;

/// <summary>
/// One entry of the Price history screen's room picker.
/// </summary>
/// <param name="RoomCategoryId">The room category's id.</param>
/// <param name="Name">The room name.</param>
public sealed record PriceHistoryRoomOption(Guid RoomCategoryId, string Name);
