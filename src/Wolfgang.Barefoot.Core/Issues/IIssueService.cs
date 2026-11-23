using Wolfgang.Barefoot.Contracts.Issues;

namespace Wolfgang.Barefoot.Core.Issues;

/// <summary>
/// The caller's Open issues (ADR 0008 §7): what needs their attention until it is put right. A
/// person sees only their own; an Admin also sees the Admin-only ones.
/// </summary>
/// <remarks>
/// Nothing here raises or resolves an issue. Issues are raised by what goes wrong (a failed delivery,
/// a passed window, an offline resort) and resolved by what puts it right.
/// </remarks>
public interface IIssueService
{
    /// <summary>
    /// The caller's issues that are not resolved, the newest first. The list is short and is not paged.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<IReadOnlyList<OpenIssueResponse>> ListAsync(CancellationToken cancellationToken);



    /// <summary>
    /// Stops the pop-up showing one issue. The issue stays listed until it is resolved. Acknowledging
    /// an issue already acknowledged changes nothing.
    /// </summary>
    /// <param name="id">The issue's id. It must be one of the caller's.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<OpenIssueResponse> AcknowledgeAsync(Guid id, CancellationToken cancellationToken);



    /// <summary>
    /// Stops the pop-up showing every issue the caller has not acknowledged, and returns their
    /// unresolved issues as they then stand.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<IReadOnlyList<OpenIssueResponse>> AcknowledgeAllAsync(CancellationToken cancellationToken);
}
