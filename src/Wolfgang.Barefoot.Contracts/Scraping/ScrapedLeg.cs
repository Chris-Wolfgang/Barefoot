namespace Wolfgang.Barefoot.Contracts.Scraping;

/// <summary>
/// One flight of an itinerary, as the booking site showed it (<c>docs/API-NOTES.md</c>, "Itinerary
/// cards"). Times are local to their airport, exactly as shown; nothing is converted.
/// </summary>
/// <param name="Carrier">The carrier the flight is sold under.</param>
/// <param name="FlightNumber">The flight number, digits only as shown after "Flight #".</param>
/// <param name="OperatingCarrier">The carrier named after "Flight operated by".</param>
/// <param name="CabinName">The cabin as the site names it, for example <c>Standard Economy</c>.</param>
/// <param name="FareCode">The fare code shown in parentheses after the cabin name.</param>
/// <param name="DepartureDate">The date shown on the leg: the local date of departure.</param>
/// <param name="DepartureTime">The local departure time.</param>
/// <param name="DepartureAirportCode">The three-letter code of the departure airport.</param>
/// <param name="ArrivalTime">The local arrival time. The site shows no arrival date.</param>
/// <param name="ArrivalAirportCode">The three-letter code of the arrival airport.</param>
/// <param name="IsOvernight">Whether the leg is marked "Overnight Flight".</param>
public sealed record ScrapedLeg
(
    string Carrier,
    string FlightNumber,
    string OperatingCarrier,
    string CabinName,
    string FareCode,
    DateOnly DepartureDate,
    TimeOnly DepartureTime,
    string DepartureAirportCode,
    TimeOnly ArrivalTime,
    string ArrivalAirportCode,
    bool IsOvernight
);
