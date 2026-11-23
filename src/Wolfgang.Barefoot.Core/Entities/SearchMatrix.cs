using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// A stored search: what one person wants looked up on every run (DESIGN §4, ADR 0004). The resorts,
/// departure airports, room filters and recipients it names are rows of their own that point here.
/// </summary>
public sealed class SearchMatrix
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="AppUser"/> the search belongs to. Who may see a result follows from this.</summary>
    public required Guid OwnerId { get; set; }



    /// <summary>The name the owner gave the search.</summary>
    public required string Name { get; set; }



    /// <summary>
    /// <see cref="Name"/> as <see cref="NormalizeName"/> gives it. An owner has at most one search
    /// with a given normalized name.
    /// </summary>
    public required string NormalizedName { get; set; }



    /// <summary>The brand the search is for.</summary>
    public required Brand Brand { get; set; }



    /// <summary>Whether the search is a draft, runs, or is switched off.</summary>
    public SearchStatus Status { get; set; }



    /// <summary>
    /// When its lookups are handed out relative to other searches': a higher number goes first.
    /// </summary>
    public int RunOrder { get; set; } = SearchDefinition.NormalRunOrder;



    /// <summary>How many nights the stay is.</summary>
    public required int Nights { get; set; }



    /// <summary>How many adults travel.</summary>
    public required int Adults { get; set; }



    /// <summary>How many children travel.</summary>
    public int Children { get; set; }



    /// <summary>How many infants travel.</summary>
    public int Infants { get; set; }



    /// <summary>Which of the two kinds of check-in window the search has.</summary>
    public required WindowKind WindowKind { get; set; }



    /// <summary>The month a recurring window opens, or <see langword="null"/> for a one-time window.</summary>
    public int? RecurringFirstMonth { get; set; }



    /// <summary>The day of that month a recurring window opens, or <see langword="null"/>.</summary>
    public int? RecurringFirstDay { get; set; }



    /// <summary>The month a recurring window closes, or <see langword="null"/> for a one-time window.</summary>
    public int? RecurringLastMonth { get; set; }



    /// <summary>The day of that month a recurring window closes, or <see langword="null"/>.</summary>
    public int? RecurringLastDay { get; set; }



    /// <summary>The first check-in of a one-time window, or <see langword="null"/> for a recurring one.</summary>
    public DateOnly? FirstCheckIn { get; set; }



    /// <summary>The last check-in of a one-time window, or <see langword="null"/> for a recurring one.</summary>
    public DateOnly? LastCheckIn { get; set; }



    /// <summary>Whether flights are looked up as well as rooms.</summary>
    public bool IncludeFlights { get; set; }



    /// <summary>The cabin flights are looked up in, or <see langword="null"/> when flights are not included.</summary>
    public Cabin? Cabin { get; set; }



    /// <summary>The most the stay and flights may cost together, or <see langword="null"/> for no limit.</summary>
    public decimal? MaxTotalPrice { get; set; }



    /// <summary>The longest drive from the airport in minutes, or <see langword="null"/> for no limit.</summary>
    public int? MaxDriveMinutes { get; set; }



    /// <summary>The longest flight in minutes, or <see langword="null"/> for no limit.</summary>
    public int? MaxFlightMinutes { get; set; }



    /// <summary>The most stops a flight may make, or <see langword="null"/> for no limit.</summary>
    public int? MaxStops { get; set; }



    /// <summary>When the search was created.</summary>
    public DateTimeOffset CreatedUtc { get; set; }



    /// <summary>The concurrency token, which is also the row's ETag (ADR 0004 §10).</summary>
    public long Version { get; set; }



    /// <summary>
    /// A search name in the form uniqueness is judged by: trimmed and upper-cased without regard to
    /// culture (ADR 0004 §5). Two names that normalize alike are the same name.
    /// </summary>
    /// <param name="name">The name as the owner typed it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public static string NormalizeName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return name.Trim().ToUpperInvariant();
    }
}
