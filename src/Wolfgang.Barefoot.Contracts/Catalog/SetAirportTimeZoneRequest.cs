namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// The time zone an Admin gives an airport (DESIGN §7).
/// </summary>
/// <param name="TimeZoneId">The IANA time zone, for example <c>America/St_Vincent</c>.</param>
public sealed record SetAirportTimeZoneRequest(string TimeZoneId);
