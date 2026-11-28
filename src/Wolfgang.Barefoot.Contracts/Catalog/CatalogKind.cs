namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// The two catalogs the scraper sends as snapshots (DESIGN §5).
/// </summary>
public enum CatalogKind
{
    /// <summary>The airports the booking site knows.</summary>
    Airports = 0,

    /// <summary>The resorts the booking site offers.</summary>
    Resorts = 1,
}
