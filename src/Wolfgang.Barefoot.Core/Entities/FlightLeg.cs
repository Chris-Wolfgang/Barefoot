namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One flight of a <see cref="FlightDirection"/>, as the site gave it. The times are local to each
/// airport and carry no zone; converting them needs the airports' time zones.
/// </summary>
public sealed class FlightLeg
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="FlightDirection"/> the flight is part of.</summary>
    public required Guid FlightDirectionId { get; set; }



    /// <summary>The flight's place in the direction, from 1.</summary>
    public required int Sequence { get; set; }



    /// <summary>The airline that sells the flight.</summary>
    public required string Carrier { get; set; }



    /// <summary>The flight number.</summary>
    public required string FlightNumber { get; set; }



    /// <summary>The airline that flies it.</summary>
    public required string OperatingCarrier { get; set; }



    /// <summary>The cabin as the airline names it.</summary>
    public required string CabinName { get; set; }



    /// <summary>The fare code.</summary>
    public required string FareCode { get; set; }



    /// <summary>The date the flight departs, at the departure airport.</summary>
    public required DateOnly DepartureDate { get; set; }



    /// <summary>The time the flight departs, at the departure airport.</summary>
    public required TimeOnly DepartureTime { get; set; }



    /// <summary>The code of the airport it departs from.</summary>
    public required string DepartureAirportCode { get; set; }



    /// <summary>The time the flight arrives, at the arrival airport.</summary>
    public required TimeOnly ArrivalTime { get; set; }



    /// <summary>The code of the airport it arrives at.</summary>
    public required string ArrivalAirportCode { get; set; }



    /// <summary>Whether the site marks the flight as arriving the next day.</summary>
    public bool Overnight { get; set; }
}
