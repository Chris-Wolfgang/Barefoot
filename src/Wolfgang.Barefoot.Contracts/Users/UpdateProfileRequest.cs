namespace Wolfgang.Barefoot.Contracts.Users;

/// <summary>
/// The preferences a person sets for themselves on Profile (ADR 0004 §8). Their name and address
/// come from their sign-in and are not among them.
/// </summary>
/// <param name="TimeZoneId">
/// The IANA time zone to show instants in, or <see langword="null"/> to use the zone of the device
/// they are on.
/// </param>
/// <param name="DefaultAdultCount">The number of adults a new search starts with. At least one.</param>
/// <param name="DefaultChildCount">The number of children a new Beaches search starts with.</param>
/// <param name="DefaultInfantCount">The number of infants a new Beaches search starts with.</param>
public sealed record UpdateProfileRequest
(
    string? TimeZoneId,
    int DefaultAdultCount,
    int DefaultChildCount = 0,
    int DefaultInfantCount = 0
);
