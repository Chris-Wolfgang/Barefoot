namespace Wolfgang.Barefoot.Core.Searches;

/// <summary>
/// What a person entered for a search, before it is known to be valid: the two halves of DESIGN §4,
/// "what to search" and the digest filters, plus the run order.
/// </summary>
/// <remarks>
/// The values are held as they were given, so <see cref="SearchValidator"/> can report every rule that
/// is broken instead of the first one a constructor would throw on. The check-in window is therefore
/// its raw fields here; <see cref="SearchValidator.BuildWindow"/> turns a valid definition into a
/// <see cref="CheckInWindow"/>.
/// </remarks>
public sealed record SearchDefinition
{
    /// <summary>The search's name, unique per owner.</summary>
    public required string Name { get; init; }



    /// <summary>The brand the search is for.</summary>
    public required Brand Brand { get; init; }



    /// <summary>The resorts to look up. At least one.</summary>
    public IReadOnlyList<Guid> ResortIds { get; init; } = [];



    /// <summary>The length of the stay in nights. At least one.</summary>
    public required int Nights { get; init; }



    /// <summary>The number of adults. At least one.</summary>
    public required int Adults { get; init; }



    /// <summary>The number of children. Only a Beaches search may have any.</summary>
    public int Children { get; init; }



    /// <summary>The number of infants. Only a Beaches search may have any.</summary>
    public int Infants { get; init; }



    /// <summary>Which kind of check-in window the fields below describe.</summary>
    public required WindowKind WindowKind { get; init; }



    /// <summary>Recurring window: the month of the first check-in day.</summary>
    public int? RecurringFirstMonth { get; init; }



    /// <summary>Recurring window: the day of the month of the first check-in day.</summary>
    public int? RecurringFirstDay { get; init; }



    /// <summary>Recurring window: the month of the last check-in day.</summary>
    public int? RecurringLastMonth { get; init; }



    /// <summary>Recurring window: the day of the month of the last check-in day.</summary>
    public int? RecurringLastDay { get; init; }



    /// <summary>One-time window: the first check-in date.</summary>
    public DateOnly? FirstCheckIn { get; init; }



    /// <summary>One-time window: the last check-in date.</summary>
    public DateOnly? LastCheckIn { get; init; }



    /// <summary>Whether flights are looked up as well as rooms.</summary>
    public bool IncludeFlights { get; init; }



    /// <summary>The cabin flights are looked up in. Set only when flights are included.</summary>
    public Cabin? Cabin { get; init; }



    /// <summary>The departure airports. At least one when flights are included, none otherwise.</summary>
    public IReadOnlyList<Guid> DepartureAirportIds { get; init; } = [];



    /// <summary>Digest filter: the highest total price to show. Greater than zero when set.</summary>
    public decimal? MaxTotalPrice { get; init; }



    /// <summary>Digest filter: the longest drive from the gateway airport to the resort, in minutes.</summary>
    public int? MaxDriveMinutes { get; init; }



    /// <summary>Digest filter, flights only: the longest flight time including layovers, in minutes.</summary>
    public int? MaxFlightMinutes { get; init; }



    /// <summary>Digest filter, flights only: the most stops. Zero means non-stop only.</summary>
    public int? MaxStops { get; init; }



    /// <summary>
    /// When the search runs relative to the owner's others: 1 (last) to 99 (first). 50 is normal.
    /// </summary>
    public int RunOrder { get; init; } = SearchValidator.NormalRunOrder;
}
