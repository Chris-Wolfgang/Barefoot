namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One way to fly there and back that the site offered for a <see cref="FlightSet"/>: an outbound
/// and a return <see cref="FlightDirection"/>, and what they cost.
/// </summary>
public sealed class FlightOption
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="FlightSet"/> the option was offered for.</summary>
    public required Guid FlightSetId { get; set; }



    /// <summary>
    /// The airfare for the whole party in US dollars: the site's combined total less the room price
    /// (DESIGN §6).
    /// </summary>
    public required decimal Fare { get; set; }
}
