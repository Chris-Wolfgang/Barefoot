using Wolfgang.Barefoot.Contracts.Catalog;

namespace Wolfgang.Barefoot.Core.Catalog;

/// <summary>
/// Keeps the catalog of airports and resorts in step with the booking site (DESIGN §5 and §7). The
/// scraper posts what the site shows; the API decides what changes.
/// </summary>
/// <remarks>
/// Refresh the airports before the resorts, so that a resort's gateway airport exists when the
/// resort is written.
/// </remarks>
public interface ICatalogService
{
    /// <summary>
    /// Applies a snapshot of the site's airports.
    /// </summary>
    /// <param name="snapshot">Every airport the site showed.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<CatalogRefreshResponse> RefreshAirportsAsync(AirportSnapshot snapshot, CancellationToken cancellationToken);



    /// <summary>
    /// Applies a snapshot of the site's resorts.
    /// </summary>
    /// <param name="snapshot">Every resort the site showed.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<CatalogRefreshResponse> RefreshResortsAsync(ResortSnapshot snapshot, CancellationToken cancellationToken);
}
