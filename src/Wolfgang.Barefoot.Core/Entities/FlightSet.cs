using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One flight lookup of a run: from one airport to a gateway and back, on two dates, in one cabin,
/// for one party (DESIGN §6). It is not tied to a resort, because resorts on one island share a
/// gateway. Within a run the lookup is unique, and its options hang off it.
/// </summary>
public sealed class FlightSet
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="Run"/> the lookup belongs to.</summary>
    public required Guid RunId { get; set; }



    /// <summary>The <see cref="Airport"/> the party departs from.</summary>
    public required Guid OriginAirportId { get; set; }



    /// <summary>The <see cref="Airport"/> the party flies into.</summary>
    public required Guid GatewayAirportId { get; set; }



    /// <summary>The date of the outbound flight.</summary>
    public required DateOnly DepartDate { get; set; }



    /// <summary>The date of the return flight.</summary>
    public required DateOnly ReturnDate { get; set; }



    /// <summary>The cabin looked up.</summary>
    public required Cabin Cabin { get; set; }



    /// <summary>How many adults travel.</summary>
    public required int Adults { get; set; }



    /// <summary>How many children travel.</summary>
    public int Children { get; set; }



    /// <summary>How many infants travel.</summary>
    public int Infants { get; set; }



    /// <summary>Where the lookup stands.</summary>
    public required WorkStatus Status { get; set; }



    /// <summary>The error the worker last reported, or <see langword="null"/>.</summary>
    public string? LastError { get; set; }



    /// <summary>When the result was captured, or <see langword="null"/> while there is none.</summary>
    public DateTimeOffset? CapturedUtc { get; set; }



    /// <summary>The worker that holds the lease, or <see langword="null"/> when nobody does.</summary>
    public string? LeasedBy { get; set; }



    /// <summary>When that lease runs out, or <see langword="null"/> when nobody holds one.</summary>
    public DateTimeOffset? LeaseExpiresUtc { get; set; }



    /// <summary>
    /// The token minted by the claim that last handed the lookup out, or <see langword="null"/> before
    /// the first claim. A result is accepted only with the current token.
    /// </summary>
    public Guid? Attempt { get; set; }



    /// <summary>The SHA-256 of the stored result, or <see langword="null"/> while there is none.</summary>
    public string? ResultSha256 { get; set; }



    /// <summary>The concurrency token (ADR 0004 §10).</summary>
    public long Version { get; set; }
}
