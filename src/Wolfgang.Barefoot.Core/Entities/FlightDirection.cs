namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One direction of a <see cref="FlightOption"/>: the way out or the way back, as one or more
/// <see cref="FlightLeg"/> rows.
/// </summary>
public sealed class FlightDirection
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="FlightOption"/> the direction is part of.</summary>
    public required Guid FlightOptionId { get; set; }



    /// <summary>Whether this is the way back. An option has one direction each way.</summary>
    public required bool IsReturn { get; set; }



    /// <summary>Whether the site marks the direction as overnight.</summary>
    public bool Overnight { get; set; }
}
