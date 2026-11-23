namespace Wolfgang.Barefoot.Core.Digests;

/// <summary>
/// One room option in a digest: one card (DESIGN §9).
/// </summary>
/// <param name="TotalAmount">The price shown large: the room, plus the cheapest flights when the search includes flights.</param>
/// <param name="TotalNote">What the total includes, for example "with flights from PHL"; <see langword="null"/> for none.</param>
/// <param name="RoomName">The room's name.</param>
/// <param name="Amenities">The room's level, view, type and features, in the order to show them.</param>
/// <param name="CheckIn">The check-in date.</param>
/// <param name="NightCount">The length of the stay.</param>
/// <param name="RoomAmount">The room's price alone.</param>
/// <param name="PreviousRoomAmount">The price the site struck through, or <see langword="null"/> when it showed none.</param>
/// <param name="Promotion">The promotion the site named, or <see langword="null"/> when it named none.</param>
/// <param name="Flights">The cheapest airfare per departure airport, cheapest first; empty without flights.</param>
public sealed record DigestRoom
(
    decimal TotalAmount,
    string? TotalNote,
    string RoomName,
    IReadOnlyList<string> Amenities,
    DateOnly CheckIn,
    int NightCount,
    decimal RoomAmount,
    decimal? PreviousRoomAmount,
    string? Promotion,
    IReadOnlyList<DigestFlightPrice> Flights
);
