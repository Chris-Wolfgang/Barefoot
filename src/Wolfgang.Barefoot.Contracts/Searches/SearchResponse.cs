namespace Wolfgang.Barefoot.Contracts.Searches;

/// <summary>
/// A stored search, as the API returns it.
/// </summary>
/// <param name="Id">The search's id, minted by the API (ADR 0012 §4).</param>
/// <param name="OwnerId">The user the search belongs to.</param>
/// <param name="Status">Where the search is in its life.</param>
/// <param name="Version">
/// The row version. It is the search's ETag: a change or a delete must name the version it was read
/// at (ADR 0012 §3).
/// </param>
/// <param name="Definition">What the search looks for and what its digest shows.</param>
public sealed record SearchResponse
(
    Guid Id,
    Guid OwnerId,
    SearchStatus Status,
    long Version,
    SearchDefinition Definition
);
