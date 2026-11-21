namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// How current the reference data is: the "last refreshed" line of the Reference data page.
/// </summary>
/// <param name="LastRefreshedUtc">
/// When a catalog snapshot was last applied, or <see langword="null"/> when none ever was.
/// </param>
public sealed record CatalogStatusResponse(DateTimeOffset? LastRefreshedUtc);
