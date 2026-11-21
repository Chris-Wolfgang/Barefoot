namespace Wolfgang.Barefoot.Contracts.Scraping;

/// <summary>
/// The body of <c>POST /runs/{id}/flight-sets/{f}/result</c>: the round trips the scraper found for
/// one flight set, or why it found none (DESIGN §5).
/// </summary>
/// <param name="Attempt">The attempt token <c>claim</c> minted; a repeat with the same token is answered once (ADR 0012 §4).</param>
/// <param name="Options">The round trips, or <see langword="null"/> when the lookup failed.</param>
/// <param name="Error">What went wrong, or <see langword="null"/> when the lookup succeeded.</param>
public sealed record FlightsResult
(
    Guid Attempt,
    IReadOnlyList<ScrapedItinerary>? Options,
    string? Error
);
