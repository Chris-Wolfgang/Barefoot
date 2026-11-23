namespace Wolfgang.Barefoot.Contracts.Results;

/// <summary>
/// The cheapest total of a run for one search, and where it is: "$6,142 (Grande St. Lucian, Feb 24,
/// from PHL)".
/// </summary>
/// <param name="Total">The room price plus the cheapest airfare, or the room price alone without flights.</param>
/// <param name="ResortName">The resort's short name.</param>
/// <param name="CheckIn">The check-in date.</param>
/// <param name="AirportCode">
/// The departure airport the airfare is from, or <see langword="null"/> when the total has no flights.
/// </param>
public sealed record CheapestTotalResponse
(
    decimal Total,
    string ResortName,
    DateOnly CheckIn,
    string? AirportCode
);
