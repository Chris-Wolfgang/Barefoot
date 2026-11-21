namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// An airport, as the API returns it (DESIGN §7). The code and the name are the booking site's facts;
/// the time zone comes from a list built into the API, or from the Admin when that list does not
/// know the airport.
/// </summary>
/// <param name="Id">The airport's id.</param>
/// <param name="Code">The three-letter airport code.</param>
/// <param name="Name">The airport's name.</param>
/// <param name="TimeZoneId">
/// The IANA time zone, or <see langword="null"/> when none is known. Until one is set the airport's
/// flight times are shown as the site gave them.
/// </param>
/// <param name="IsDeparture">Whether the site offers the airport to fly from.</param>
/// <param name="IsGateway">Whether a resort is reached through the airport.</param>
/// <param name="FirstSeenUtc">When a snapshot first held the airport.</param>
/// <param name="SearchCount">
/// The number of searches that use the airport. Only an Admin is told; it is
/// <see langword="null"/> for anyone else.
/// </param>
/// <param name="Version">The row version: the airport's ETag (ADR 0012 §3).</param>
public sealed record AirportResponse
(
    Guid Id,
    string Code,
    string Name,
    string? TimeZoneId,
    bool IsDeparture,
    bool IsGateway,
    DateTimeOffset FirstSeenUtc,
    int? SearchCount,
    long Version
);
