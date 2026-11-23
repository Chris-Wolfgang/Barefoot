namespace Wolfgang.Barefoot.Contracts.Scraping;

/// <summary>
/// One round trip the booking site offered for a flight set (<c>docs/API-NOTES.md</c>, "Itinerary cards").
/// </summary>
/// <param name="TotalAmount">
/// The "room + flights" total on the card, in US dollars. The site shows no air-only figure: the
/// API derives the airfare by subtracting the room price (DESIGN §6).
/// </param>
/// <param name="RoomCategoryCode">The category code of the room the total includes, so the API knows which room price to subtract.</param>
/// <param name="Outbound">The way out.</param>
/// <param name="Return">The way back.</param>
public sealed record ScrapedItinerary
(
    decimal TotalAmount,
    string RoomCategoryCode,
    ScrapedDirection Outbound,
    ScrapedDirection Return
);
