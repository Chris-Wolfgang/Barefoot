namespace Wolfgang.Barefoot.Contracts.Digests;

/// <summary>
/// The body that adds or changes an entry of an owner's address book (ADR 0006 §2).
/// </summary>
/// <param name="Address">The e-mail address. It may not be the owner's own sign-in address.</param>
/// <param name="DisplayName">The name shown for the address.</param>
public sealed record RecipientRequest(string Address, string DisplayName);
