namespace Wolfgang.Barefoot.Contracts.Digests;

/// <summary>
/// One entry of an owner's address book: an extra address a search of theirs may mail (ADR 0006 §2).
/// </summary>
/// <param name="Id">The recipient's id.</param>
/// <param name="OwnerId">The user whose address book it is in.</param>
/// <param name="Address">The e-mail address, trimmed and lower-cased.</param>
/// <param name="DisplayName">The name shown for the address.</param>
/// <param name="Version">The row version, which is the recipient's ETag (ADR 0012 §3).</param>
public sealed record RecipientResponse
(
    Guid Id,
    Guid OwnerId,
    string Address,
    string DisplayName,
    long Version
);
