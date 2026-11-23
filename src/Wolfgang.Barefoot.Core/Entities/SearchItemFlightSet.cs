namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// The link that says which flight lookup serves one search item (ADR 0004 §6). An item has one
/// link per departure airport of its search.
/// </summary>
public sealed class SearchItemFlightSet
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="SearchItem"/> that wants the flights.</summary>
    public required Guid SearchItemId { get; set; }



    /// <summary>The <see cref="FlightSet"/> that holds them.</summary>
    public required Guid FlightSetId { get; set; }
}
