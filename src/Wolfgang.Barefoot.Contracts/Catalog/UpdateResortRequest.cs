namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// The two values of a resort an Admin enters, because the booking site gives neither (DESIGN §7).
/// </summary>
/// <param name="ShortName">The name used in lists and digests.</param>
/// <param name="DriveMinutes">The drive from the gateway airport to the resort, in minutes.</param>
public sealed record UpdateResortRequest(string ShortName, int DriveMinutes);
