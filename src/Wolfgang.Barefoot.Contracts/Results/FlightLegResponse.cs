namespace Wolfgang.Barefoot.Contracts.Results;

/// <summary>
/// One flight of a round trip. Times are local to their airport, as the booking site showed them.
/// </summary>
/// <param name="Carrier">The carrier the flight is sold under.</param>
/// <param name="FlightNumber">The flight number.</param>
/// <param name="OperatingCarrier">The carrier that operates the flight.</param>
/// <param name="CabinName">The cabin as the site names it.</param>
/// <param name="DepartureDate">The local date of departure.</param>
/// <param name="DepartureTime">The local departure time.</param>
/// <param name="DepartureAirportCode">The three-letter code of the departure airport.</param>
/// <param name="ArrivalTime">The local arrival time.</param>
/// <param name="ArrivalAirportCode">The three-letter code of the arrival airport.</param>
/// <param name="IsOvernight">Whether the site marks the flight as overnight.</param>
/// <param name="LayoverMinutes">
/// The wait at the arrival airport before the next flight of the direction. It is
/// <see langword="null"/> after the last flight, and when it cannot be worked out.
/// </param>
public sealed record FlightLegResponse
(
    string Carrier,
    string FlightNumber,
    string OperatingCarrier,
    string CabinName,
    DateOnly DepartureDate,
    TimeOnly DepartureTime,
    string DepartureAirportCode,
    TimeOnly ArrivalTime,
    string ArrivalAirportCode,
    bool IsOvernight,
    int? LayoverMinutes
);
