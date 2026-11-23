namespace Wolfgang.Barefoot.Contracts.Results;

/// <summary>
/// The flights of one departure airport for one room option, as the grid's airport button shows them:
/// the fare range and the number of options. The options themselves are read a page at a time by
/// <paramref name="FlightSetId"/> (DESIGN §6).
/// </summary>
/// <param name="FlightSetId">The flight set the options belong to.</param>
/// <param name="AirportCode">The three-letter code of the departure airport.</param>
/// <param name="CheapestFare">The lowest airfare among the options.</param>
/// <param name="DearestFare">The highest airfare among the options.</param>
/// <param name="OptionCount">The number of round trips found.</param>
public sealed record ResultFlightsResponse
(
    Guid FlightSetId,
    string AirportCode,
    decimal CheapestFare,
    decimal DearestFare,
    int OptionCount
);
