namespace Wolfgang.Barefoot.Core.Digests;

/// <summary>
/// The cheapest airfare from one departure airport, as a digest shows it beside a room.
/// </summary>
/// <param name="AirportCode">The departure airport's code.</param>
/// <param name="AirfareAmount">The airfare for all travellers, in US dollars.</param>
public sealed record DigestFlightPrice(string AirportCode, decimal AirfareAmount);
