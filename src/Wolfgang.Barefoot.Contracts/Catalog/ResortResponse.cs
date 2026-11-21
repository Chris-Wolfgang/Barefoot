using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// A resort, as the API returns it (DESIGN §7). The code, name, brand, island, gateway and whether it
/// is listed are the booking site's facts; the short name and the drive time are the Admin's, because
/// the site gives neither.
/// </summary>
/// <param name="Id">The resort's id.</param>
/// <param name="Code">The resort's code on the site.</param>
/// <param name="Name">The resort's full name.</param>
/// <param name="ShortName">
/// The name used in lists and digests, or <see langword="null"/> for a newly seen resort nobody has named yet.
/// </param>
/// <param name="Brand">The brand the resort belongs to.</param>
/// <param name="Island">The island the resort is on.</param>
/// <param name="GatewayAirportCode">
/// The code of the airport the resort is reached through, or <see langword="null"/> when the site gave none.
/// </param>
/// <param name="DriveMinutes">
/// The drive from the gateway airport to the resort, or <see langword="null"/> when nobody has entered it yet.
/// </param>
/// <param name="OfflineSince">
/// The date the site stopped listing the resort, or <see langword="null"/> while it is listed.
/// </param>
/// <param name="FirstSeenUtc">When a snapshot first held the resort.</param>
/// <param name="SearchCount">
/// The number of searches that include the resort. Only an Admin is told; it is
/// <see langword="null"/> for anyone else, who must not learn what other people search for.
/// </param>
/// <param name="Version">The row version: the resort's ETag (ADR 0012 §3).</param>
public sealed record ResortResponse
(
    Guid Id,
    string Code,
    string Name,
    string? ShortName,
    Brand Brand,
    string Island,
    string? GatewayAirportCode,
    int? DriveMinutes,
    DateOnly? OfflineSince,
    DateTimeOffset FirstSeenUtc,
    int? SearchCount,
    long Version
);
