namespace Wolfgang.Barefoot.Contracts.Results;

/// <summary>
/// One round trip of a flight set, with its legs (DESIGN §6).
/// </summary>
/// <param name="Id">The id of the stored option.</param>
/// <param name="Fare">The airfare: the site's room-and-flights total less the room price.</param>
/// <param name="Outbound">The way out.</param>
/// <param name="Return">The way back.</param>
public sealed record FlightOptionResponse
(
    Guid Id,
    decimal Fare,
    FlightDirectionResponse Outbound,
    FlightDirectionResponse Return
);
