namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One address in a person's address book: someone, other than the owner, a search of theirs can
/// mail its digest to (ADR 0006). An address appears once per owner.
/// </summary>
public sealed class Recipient
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="AppUser"/> whose address book this is.</summary>
    public required Guid OwnerId { get; set; }



    /// <summary>The e-mail address, trimmed and lower-cased.</summary>
    public required string Address { get; set; }



    /// <summary>The name the owner gave the recipient.</summary>
    public required string DisplayName { get; set; }



    /// <summary>The concurrency token, which is also the row's ETag (ADR 0004 §10).</summary>
    public long Version { get; set; }
}
