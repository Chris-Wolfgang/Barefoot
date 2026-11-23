namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// A person who has signed in at least once (ADR 0004 §8). The row gives lists a name without a
/// directory call, gives an owner something to point at, and holds the person's own settings.
/// </summary>
/// <remarks>
/// A person's role is not stored here: it comes from the directory with each sign-in (ADR 0005).
/// </remarks>
public sealed class AppUser
{
    /// <summary>
    /// The key of the seeded "Unassigned" user, who owns whatever is created before the application
    /// is connected to the directory. No real object id equals it.
    /// </summary>
    public static readonly Guid UnassignedOid = Guid.Empty;

    /// <summary>The display name of the seeded "Unassigned" user.</summary>
    public const string UnassignedDisplayName = "Unassigned";



    /// <summary>The person's object id in the directory. It is the row's key.</summary>
    public required Guid Oid { get; set; }



    /// <summary>The person's name as the directory gave it at the last sign-in.</summary>
    public required string DisplayName { get; set; }



    /// <summary>The person's e-mail address as the directory gave it at the last sign-in.</summary>
    public required string Email { get; set; }



    /// <summary>When the person last signed in.</summary>
    public DateTimeOffset LastSeenUtc { get; set; }



    /// <summary>
    /// The version of the terms the person accepted, or <see langword="null"/> when they have
    /// accepted none.
    /// </summary>
    public int? DisclaimerVersionAccepted { get; set; }



    /// <summary>When the person accepted that version, or <see langword="null"/>.</summary>
    public DateTimeOffset? DisclaimerAcceptedUtc { get; set; }



    /// <summary>
    /// The IANA time zone the person reads times in, or <see langword="null"/> when they have chosen
    /// none.
    /// </summary>
    public string? TimeZoneId { get; set; }



    /// <summary>How many adults a new search of theirs starts with.</summary>
    public int DefaultAdults { get; set; }



    /// <summary>How many children a new search of theirs starts with.</summary>
    public int DefaultChildren { get; set; }



    /// <summary>How many infants a new search of theirs starts with.</summary>
    public int DefaultInfants { get; set; }



    /// <summary>
    /// A sign-in older than this instant is no longer honoured, or <see langword="null"/> when no
    /// Admin has signed the person out everywhere (ADR 0005).
    /// </summary>
    public DateTimeOffset? SessionsValidAfterUtc { get; set; }



    /// <summary>The concurrency token, which is also the row's ETag (ADR 0004 §10).</summary>
    public long Version { get; set; }
}
