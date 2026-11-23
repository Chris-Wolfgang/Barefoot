using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// A resort's permanent identity (DESIGN §7). Whether it can be booked on a given day is not here:
/// that is its <see cref="ResortSpan"/> rows.
/// </summary>
public sealed class Resort
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The site's code for the resort.</summary>
    public required string Code { get; set; }



    /// <summary>The resort's name as the site gives it.</summary>
    public required string Name { get; set; }



    /// <summary>
    /// The name lists and digests use, or <see langword="null"/> when no Admin has given one and the
    /// full name is used.
    /// </summary>
    public string? ShortName { get; set; }



    /// <summary>The brand the resort belongs to.</summary>
    public required Brand Brand { get; set; }



    /// <summary>The island or country the resort is on.</summary>
    public required string Island { get; set; }



    /// <summary>
    /// The <see cref="Airport"/> guests fly into, or <see langword="null"/> when the site names none
    /// the catalog knows.
    /// </summary>
    public Guid? GatewayAirportId { get; set; }



    /// <summary>
    /// The drive from that airport in minutes, or <see langword="null"/> until an Admin enters it.
    /// </summary>
    public int? DriveMinutes { get; set; }



    /// <summary>When a snapshot first listed the resort.</summary>
    public DateTimeOffset FirstSeenUtc { get; set; }



    /// <summary>The concurrency token, which is also the row's ETag (ADR 0004 §10).</summary>
    public long Version { get; set; }
}
