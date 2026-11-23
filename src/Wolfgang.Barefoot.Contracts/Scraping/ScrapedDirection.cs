namespace Wolfgang.Barefoot.Contracts.Scraping;

/// <summary>
/// One direction of a round trip: the way out or the way back (DESIGN §6).
/// </summary>
/// <param name="Legs">The flights in the order they are flown. The number of stops is one less than their number.</param>
/// <param name="Overnight">Whether the site marks the direction "Overnight".</param>
public sealed record ScrapedDirection(IReadOnlyList<ScrapedLeg> Legs, bool Overnight);
