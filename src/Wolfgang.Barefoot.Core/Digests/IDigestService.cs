using Wolfgang.Barefoot.Contracts;
using Wolfgang.Barefoot.Contracts.Digests;

namespace Wolfgang.Barefoot.Core.Digests;

/// <summary>
/// Sends, lists and resends the stored digests (ADR 0006; DESIGN §9). A digest is built and stored
/// when a search is completed for a run; nothing here builds one.
/// </summary>
public interface IDigestService
{
    /// <summary>
    /// Mails every pending digest of a run and reports how many were sent and how many failed. Safe
    /// to repeat: a repeat finds nothing pending.
    /// </summary>
    /// <param name="runId">The run.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<DigestSendResponse> SendPendingAsync(Guid runId, CancellationToken cancellationToken);



    /// <summary>
    /// One page of every delivery, for an Admin.
    /// </summary>
    /// <param name="filter">What to narrow the list to.</param>
    /// <param name="page">Which page.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<Page<DigestDeliveryResponse>> ListAsync
    (
        DigestFilter filter,
        PageRequest page,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// One page of the caller's own digests.
    /// </summary>
    /// <param name="filter">What to narrow the list to.</param>
    /// <param name="page">Which page.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<Page<DigestDeliveryResponse>> ListMineAsync
    (
        DigestFilter filter,
        PageRequest page,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// The stored body of one digest.
    /// </summary>
    /// <param name="deliveryId">The delivery.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<DigestContentResponse> GetContentAsync(Guid deliveryId, CancellationToken cancellationToken);



    /// <summary>
    /// Mails a stored digest again, unchanged, to the address it already went to, and records a
    /// further attempt. A user may resend their own; an Admin anyone's.
    /// </summary>
    /// <param name="deliveryId">The delivery.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<DigestDeliveryResponse> ResendAsync(Guid deliveryId, CancellationToken cancellationToken);
}
