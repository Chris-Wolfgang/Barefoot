namespace Wolfgang.Barefoot.Contracts.Scraping;

/// <summary>
/// The body of <c>POST /runs/{id}/requests/{r}/result</c>: what the scraper found for one room
/// lookup, or why it found nothing (DESIGN §5).
/// </summary>
/// <param name="Attempt">The attempt token <c>claim</c> minted; a repeat with the same token is answered once (ADR 0012 §4).</param>
/// <param name="Rooms">The room cards, or <see langword="null"/> when the lookup failed.</param>
/// <param name="Error">What went wrong, or <see langword="null"/> when the lookup succeeded.</param>
public sealed record RoomsResult
(
    Guid Attempt,
    IReadOnlyList<ScrapedRoom>? Rooms,
    string? Error
);
