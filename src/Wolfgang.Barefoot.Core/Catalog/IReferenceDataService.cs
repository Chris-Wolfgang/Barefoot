using Wolfgang.Barefoot.Contracts;
using Wolfgang.Barefoot.Contracts.Catalog;

namespace Wolfgang.Barefoot.Core.Catalog;

/// <summary>
/// Reads the reference data (DESIGN §7): the resorts, the airports and the room vocabularies, for the
/// search editor's pickers and for the Admin's Reference data page. An Admin may also enter the few
/// values the booking site does not give.
/// </summary>
/// <remarks>
/// Nothing here refreshes anything from the site: that is <see cref="ICatalogService"/>, driven by
/// the scraper. Every change names the <c>version</c> the row was read at (ADR 0012 §3).
/// </remarks>
public interface IReferenceDataService
{
    /// <summary>
    /// When the reference data was last refreshed from the booking site.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<CatalogStatusResponse> GetStatusAsync(CancellationToken cancellationToken);



    /// <summary>
    /// One page of the resorts, offline ones included.
    /// </summary>
    /// <param name="find">
    /// Text a resort's code, name, short name or island must contain, or <see langword="null"/> to list them all.
    /// </param>
    /// <param name="page">Which page.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<Page<ResortResponse>> ListResortsAsync
    (
        string? find,
        PageRequest page,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Sets a resort's short name and drive time. Admin only.
    /// </summary>
    /// <param name="id">The resort's id.</param>
    /// <param name="version">The version the resort was read at.</param>
    /// <param name="request">The two values.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<ResortResponse> UpdateResortAsync
    (
        Guid id,
        long version,
        UpdateResortRequest request,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// One page of the airports.
    /// </summary>
    /// <param name="find">
    /// Text an airport's code or name must contain, or <see langword="null"/> to list them all.
    /// </param>
    /// <param name="page">Which page.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<Page<AirportResponse>> ListAirportsAsync
    (
        string? find,
        PageRequest page,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Sets an airport's time zone, which resolves the Open issue raised for an airport without one
    /// (DESIGN §7). Admin only.
    /// </summary>
    /// <param name="id">The airport's id.</param>
    /// <param name="version">The version the airport was read at.</param>
    /// <param name="request">The time zone. A zone the API does not know is refused.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<AirportResponse> SetAirportTimeZoneAsync
    (
        Guid id,
        long version,
        SetAirportTimeZoneRequest request,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// One page of the room vocabulary values, stale ones included.
    /// </summary>
    /// <param name="kind">Only values of this vocabulary, or <see langword="null"/> for all four.</param>
    /// <param name="find">Text a value must contain, or <see langword="null"/> to list them all.</param>
    /// <param name="page">Which page.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<Page<VocabularyValueResponse>> ListVocabularyAsync
    (
        VocabularyKind? kind,
        string? find,
        PageRequest page,
        CancellationToken cancellationToken
    );
}
