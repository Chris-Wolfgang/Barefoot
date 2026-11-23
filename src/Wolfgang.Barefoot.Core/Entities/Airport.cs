namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// An airport the booking site knows: one a search can depart from, one a resort is reached
/// through, or both (DESIGN §7). Rows come from catalog snapshots and are never authored.
/// </summary>
public sealed class Airport
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The airport's three-letter code.</summary>
    public required string Code { get; set; }



    /// <summary>The airport's name as the site gives it.</summary>
    public required string Name { get; set; }



    /// <summary>The city the airport serves.</summary>
    public required string City { get; set; }



    /// <summary>The state or province, or <see langword="null"/> where the site gives none.</summary>
    public string? State { get; set; }



    /// <summary>The country.</summary>
    public required string Country { get; set; }



    /// <summary>
    /// The airport's IANA time zone, or <see langword="null"/> when the bundled lookup does not know
    /// the code and no Admin has set it yet. Without it, flight times are shown as the site gave them.
    /// </summary>
    public string? TimeZoneId { get; set; }



    /// <summary>Whether a search can depart from here.</summary>
    public bool IsDeparture { get; set; }



    /// <summary>Whether a resort is reached through here.</summary>
    public bool IsGateway { get; set; }



    /// <summary>When a snapshot first listed the airport.</summary>
    public DateTimeOffset FirstSeenUtc { get; set; }



    /// <summary>The concurrency token, which is also the row's ETag (ADR 0004 §10).</summary>
    public long Version { get; set; }
}
