namespace Wolfgang.Barefoot.Contracts.Users;

/// <summary>
/// A person who has signed in, as the API returns them: to themselves on Profile, and to an Admin on
/// the Users page (DESIGN §8 and §12).
/// </summary>
/// <param name="Id">The person's id: their Entra object id.</param>
/// <param name="DisplayName">Their name, from their sign-in.</param>
/// <param name="Email">Their sign-in address, from their sign-in. Their digests go to it.</param>
/// <param name="Role">The role they last signed in with.</param>
/// <param name="ActiveSearchCount">The number of their searches that are Active.</param>
/// <param name="ActiveSearchLimit">The number of Active searches one person may have.</param>
/// <param name="LastSeenUtc">When they last signed in.</param>
/// <param name="DisclaimerVersionAccepted">
/// The version of the terms they accepted, or <see langword="null"/> when they have accepted none.
/// </param>
/// <param name="DisclaimerAcceptedUtc">
/// When they accepted it, or <see langword="null"/> when they have accepted none.
/// </param>
/// <param name="TimeZoneId">
/// The IANA time zone instants are shown to them in, or <see langword="null"/> to use the zone of
/// the device they are on.
/// </param>
/// <param name="CanOwnSearch">
/// Whether a search can be created for the person: they have accepted the terms now in force
/// (ADR 0004 §8). A person who has not is left out of the owner picker.
/// </param>
/// <param name="DefaultAdults">The number of adults a new search of theirs starts with.</param>
/// <param name="DefaultChildren">The number of children a new Beaches search of theirs starts with.</param>
/// <param name="DefaultInfants">The number of infants a new Beaches search of theirs starts with.</param>
/// <param name="SessionsValidAfterUtc">
/// The instant set by the last "Sign out everywhere": a sign-in older than it is no longer accepted.
/// It is <see langword="null"/> when that was never done.
/// </param>
/// <param name="Version">The row version: the person's ETag (ADR 0012 §3).</param>
public sealed record UserResponse
(
    Guid Id,
    string DisplayName,
    string Email,
    UserRole Role,
    int ActiveSearchCount,
    int ActiveSearchLimit,
    DateTimeOffset LastSeenUtc,
    int? DisclaimerVersionAccepted,
    DateTimeOffset? DisclaimerAcceptedUtc,
    bool CanOwnSearch,
    string? TimeZoneId,
    int DefaultAdults,
    int DefaultChildren,
    int DefaultInfants,
    DateTimeOffset? SessionsValidAfterUtc,
    long Version
);
