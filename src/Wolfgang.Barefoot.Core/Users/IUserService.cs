using Wolfgang.Barefoot.Contracts;
using Wolfgang.Barefoot.Contracts.Users;

namespace Wolfgang.Barefoot.Core.Users;

/// <summary>
/// The people who have signed in (ADR 0004 §8, ADR 0005): a person's own row, and for an Admin
/// everyone's. Inviting someone and giving them a role is done in Entra, never here.
/// </summary>
/// <remarks>
/// A person's address book is <c>IRecipientService</c>. Every change names the <c>version</c> the row
/// was read at (ADR 0012 §3), except the two a repeat cannot harm: a sign-in and an acceptance.
/// </remarks>
public interface IUserService
{
    /// <summary>
    /// Records that the caller signed in: creates their row the first time, and afterwards brings
    /// their name, address, role and last-seen instant up to date from their sign-in. Safe to repeat.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<UserResponse> SignInAsync(CancellationToken cancellationToken);



    /// <summary>
    /// The caller's own row: "who am I", and the Profile page.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<UserResponse> GetCurrentAsync(CancellationToken cancellationToken);



    /// <summary>
    /// Records that the caller accepted the terms of use. Accepting the version already accepted
    /// changes nothing; a version other than the current one is refused.
    /// </summary>
    /// <param name="request">The version the caller ticked.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<UserResponse> AcceptDisclaimerAsync(AcceptDisclaimerRequest request, CancellationToken cancellationToken);



    /// <summary>
    /// Changes the caller's own preferences.
    /// </summary>
    /// <param name="version">The version the caller's row was read at.</param>
    /// <param name="request">The preferences. A time zone the API does not know is refused.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<UserResponse> UpdateProfileAsync
    (
        long version,
        UpdateProfileRequest request,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// One page of everyone who has signed in. Admin only.
    /// </summary>
    /// <param name="filter">Which people.</param>
    /// <param name="page">Which page.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<Page<UserResponse>> ListAsync
    (
        UserFilter filter,
        PageRequest page,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// One person. Admin only.
    /// </summary>
    /// <param name="id">The person's id.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<UserResponse> GetAsync(Guid id, CancellationToken cancellationToken);



    /// <summary>
    /// Ends every session a person has: a sign-in older than now is no longer accepted, so they sign
    /// in again and get their current role (ADR 0005). Admin only, and audited.
    /// </summary>
    /// <param name="id">The person's id.</param>
    /// <param name="version">The version the person's row was read at.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<UserResponse> SignOutEverywhereAsync
    (
        Guid id,
        long version,
        CancellationToken cancellationToken
    );
}
