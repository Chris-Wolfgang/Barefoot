namespace Wolfgang.Barefoot.Contracts.Users;

/// <summary>
/// A person's acceptance of the terms of use (ADR 0008 §6). It names the version they were shown, so
/// that accepting an old text does not count for a newer one.
/// </summary>
/// <param name="Version">The version of the terms the person ticked.</param>
public sealed record AcceptDisclaimerRequest(int Version);
