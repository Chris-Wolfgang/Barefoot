using Wolfgang.Barefoot.Contracts.Digests;

namespace Wolfgang.Barefoot.Core.Digests;

/// <summary>
/// An owner's address book: the extra addresses their searches may mail (ADR 0006 §2). The owner may
/// read their own; only an Admin may change one.
/// </summary>
public interface IRecipientService
{
    /// <summary>
    /// Every recipient in a user's address book.
    /// </summary>
    /// <param name="ownerId">The user.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<IReadOnlyList<RecipientResponse>> ListAsync(Guid ownerId, CancellationToken cancellationToken);



    /// <summary>
    /// Adds a recipient. An address the owner already has is refused, and so is one over the owner's limit.
    /// </summary>
    /// <param name="ownerId">The user.</param>
    /// <param name="request">The address and its name.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<RecipientResponse> CreateAsync
    (
        Guid ownerId,
        RecipientRequest request,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Changes a recipient. Every search that mails it follows.
    /// </summary>
    /// <param name="ownerId">The user.</param>
    /// <param name="recipientId">The recipient.</param>
    /// <param name="version">The version the recipient was read at.</param>
    /// <param name="request">The new address and name.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<RecipientResponse> UpdateAsync
    (
        Guid ownerId,
        Guid recipientId,
        long version,
        RecipientRequest request,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Removes a recipient, and with it its use by any search. Digests already stored keep the address.
    /// </summary>
    /// <param name="ownerId">The user.</param>
    /// <param name="recipientId">The recipient.</param>
    /// <param name="version">The version the recipient was read at.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task DeleteAsync
    (
        Guid ownerId,
        Guid recipientId,
        long version,
        CancellationToken cancellationToken
    );
}
