using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// One flight set leased to a worker: which round trips to look up, and the token its result must
/// carry (ADR 0003; DESIGN §6).
/// </summary>
/// <param name="FlightSetId">The flight set's id.</param>
/// <param name="Attempt">The attempt token minted by this claim.</param>
/// <param name="LeaseExpiresUtc">When the lease ends; after it the flight set returns to the others.</param>
/// <param name="ResortCode">A resort served by the gateway. The site shows flights only after a room is chosen.</param>
/// <param name="DepartureAirportCode">The code of the airport the trip starts from.</param>
/// <param name="GatewayAirportCode">The code of the airport the resort is reached through.</param>
/// <param name="DepartDate">The date of the way out.</param>
/// <param name="ReturnDate">The date of the way back.</param>
/// <param name="Cabin">The cabin to look up.</param>
/// <param name="Adults">The number of adults.</param>
/// <param name="Children">The number of children.</param>
/// <param name="Infants">The number of infants.</param>
public sealed record ClaimedFlightSet
(
    Guid FlightSetId,
    Guid Attempt,
    DateTimeOffset LeaseExpiresUtc,
    string ResortCode,
    string DepartureAirportCode,
    string GatewayAirportCode,
    DateOnly DepartDate,
    DateOnly ReturnDate,
    Cabin Cabin,
    int Adults,
    int Children,
    int Infants
);
