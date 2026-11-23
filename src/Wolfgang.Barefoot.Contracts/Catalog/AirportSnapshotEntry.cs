namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// One airport as the booking site lists it (<c>docs/API-NOTES.md</c>, "Catalogs"). The site gives no
/// time zone; the API adds it (DESIGN §7).
/// </summary>
/// <param name="Code">The three-letter airport code.</param>
/// <param name="Name">The airport's name.</param>
/// <param name="City">The city the site lists it under.</param>
/// <param name="State">The state or province, or <see langword="null"/> when the site gives none.</param>
/// <param name="Country">The country.</param>
public sealed record AirportSnapshotEntry
(
    string Code,
    string Name,
    string City,
    string? State,
    string Country
);
