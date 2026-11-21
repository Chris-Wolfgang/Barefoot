namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// What applying a snapshot changed (DESIGN §5).
/// </summary>
/// <param name="Added">How many entries were new.</param>
/// <param name="Updated">How many entries changed.</param>
/// <param name="Deactivated">How many entries are no longer offered.</param>
/// <param name="Warnings">
/// One sentence per entry that was left untouched, for example a code that appeared twice with
/// different details. The scraper logs them and exits with the partial code.
/// </param>
public sealed record CatalogRefreshResponse
(
    int Added,
    int Updated,
    int Deactivated,
    IReadOnlyList<string> Warnings
);
