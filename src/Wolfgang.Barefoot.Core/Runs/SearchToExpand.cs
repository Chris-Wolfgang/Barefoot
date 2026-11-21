using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Searches;

namespace Wolfgang.Barefoot.Core.Runs;

/// <summary>
/// One active search as the expander needs it: already valid, with its check-in window resolved to
/// dates and its resorts carrying their gateway airports.
/// </summary>
public sealed record SearchToExpand
{
    /// <summary>The search.</summary>
    public required Guid SearchId { get; init; }



    /// <summary>The resorts to look up.</summary>
    public required IReadOnlyList<ExpandResort> Resorts { get; init; }



    /// <summary>The check-in dates to look up.</summary>
    public required ResolvedWindow Window { get; init; }



    /// <summary>The length of the stay in nights. At least one.</summary>
    public required int Nights { get; init; }



    /// <summary>The number of adults.</summary>
    public required int Adults { get; init; }



    /// <summary>The number of children.</summary>
    public int Children { get; init; }



    /// <summary>The number of infants.</summary>
    public int Infants { get; init; }



    /// <summary>The search's run order, 1 to 99.</summary>
    public required int RunOrder { get; init; }



    /// <summary>The cabin, when the search includes flights; <see langword="null"/> when it does not.</summary>
    public Cabin? Cabin { get; init; }



    /// <summary>The departure airports; empty when the search does not include flights.</summary>
    public IReadOnlyList<Guid> DepartureAirportIds { get; init; } = [];
}
