namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One room lookup of a run: a resort, a stay and who travels (ADR 0004 §1). It is looked up once
/// however many searches want it, and its results hang off it. Within a run the lookup is unique.
/// </summary>
public sealed class SearchRequest
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="Run"/> the lookup belongs to.</summary>
    public required Guid RunId { get; set; }



    /// <summary>The <see cref="Resort"/> looked up.</summary>
    public required Guid ResortId { get; set; }



    /// <summary>The check-in date.</summary>
    public required DateOnly CheckIn { get; set; }



    /// <summary>The check-out date.</summary>
    public required DateOnly CheckOut { get; set; }



    /// <summary>How many adults travel.</summary>
    public required int AdultCount { get; set; }



    /// <summary>How many children travel.</summary>
    public int ChildCount { get; set; }



    /// <summary>How many infants travel.</summary>
    public int InfantCount { get; set; }



    /// <summary>
    /// When it is handed out relative to the others: the highest run order among the searches that
    /// want it.
    /// </summary>
    public required int RunOrder { get; set; }



    /// <summary>Where the lookup stands.</summary>
    public required WorkStatus Status { get; set; }



    /// <summary>How many times it has been handed to a worker.</summary>
    public int AttemptCount { get; set; }



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



    /// <summary>
    /// The SHA-256 of the stored result, or <see langword="null"/> while there is none. A repeated
    /// result with the same hash changes nothing.
    /// </summary>
    public string? ResultSha256 { get; set; }



    /// <summary>The concurrency token (ADR 0004 §10).</summary>
    public long Version { get; set; }
}
