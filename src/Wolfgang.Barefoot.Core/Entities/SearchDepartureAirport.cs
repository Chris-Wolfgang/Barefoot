namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One airport a search looks up flights from. A search names an airport at most once.
/// </summary>
public sealed class SearchDepartureAirport
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="SearchMatrix"/> that names the airport.</summary>
    public required Guid SearchMatrixId { get; set; }



    /// <summary>The <see cref="Airport"/> it departs from.</summary>
    public required Guid AirportId { get; set; }
}
