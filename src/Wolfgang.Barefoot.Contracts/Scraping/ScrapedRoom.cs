namespace Wolfgang.Barefoot.Contracts.Scraping;

/// <summary>
/// One room card as the booking site showed it for one resort, stay and occupancy
/// (<c>docs/API-NOTES.md</c>, "Room cards"). Text is passed on as read; the API decides what it means.
/// </summary>
/// <param name="CategoryCode">The room category code, short and stable within a resort, for example <c>OWB</c>.</param>
/// <param name="Name">The room name on the card.</param>
/// <param name="Level">The category level without the word "Level", for example <c>Butler</c>.</param>
/// <param name="TotalAmount">
/// The price of the whole stay for all guests, taxes included, in US dollars; <see langword="null"/>
/// when the card shows no price, as a sold-out card does.
/// </param>
/// <param name="SoldOut">Whether the card says the room is sold out for the dates.</param>
/// <param name="RemainingRoomCount">The number in "N ROOMS LEFT", or <see langword="null"/> when the card does not say.</param>
/// <param name="View">The room view, or <see langword="null"/> when it was not read. It is not on the card.</param>
/// <param name="Type">The room type, or <see langword="null"/> when it was not read. It is not on the card.</param>
/// <param name="Features">The room features that were read; empty when none was. They are not on the card.</param>
public sealed record ScrapedRoom
(
    string CategoryCode,
    string Name,
    string Level,
    decimal? TotalAmount,
    bool SoldOut,
    int? RemainingRoomCount,
    string? View,
    string? Type,
    IReadOnlyList<string> Features
);
