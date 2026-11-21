using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// One resort as the booking site lists it (<c>docs/API-NOTES.md</c>, "Catalogs").
/// </summary>
/// <param name="Code">The resort's code on the site.</param>
/// <param name="Name">The resort's name.</param>
/// <param name="Brand">The brand the resort belongs to.</param>
/// <param name="Island">The country the site lists the resort under, which is its island.</param>
/// <param name="GatewayAirportCode">
/// The code of the airport the resort is reached through, or <see langword="null"/> when the site
/// did not give one.
/// </param>
public sealed record ResortSnapshotEntry
(
    string Code,
    string Name,
    Brand Brand,
    string Island,
    string? GatewayAirportCode
);
