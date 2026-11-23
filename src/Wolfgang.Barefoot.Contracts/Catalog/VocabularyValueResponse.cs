namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// One value of a room vocabulary (DESIGN §7). Values are discovered from results, never typed in,
/// and never deleted.
/// </summary>
/// <param name="Id">The value's id. A search's digest filters name values by it.</param>
/// <param name="Kind">Which vocabulary the value belongs to.</param>
/// <param name="Value">The value as the booking site words it.</param>
/// <param name="LastSeenUtc">When a result last carried the value.</param>
/// <param name="RunsSinceSeen">The number of runs since then: "not seen in N runs".</param>
/// <param name="IsStale">
/// Whether the value has gone unseen for the configured number of runs. A stale value is greyed in
/// the editor and stays valid in a filter.
/// </param>
/// <param name="SearchCount">
/// The number of searches that filter on the value. Only an Admin is told; it is
/// <see langword="null"/> for anyone else.
/// </param>
public sealed record VocabularyValueResponse
(
    Guid Id,
    VocabularyKind Kind,
    string Value,
    DateTimeOffset LastSeenUtc,
    int RunsSinceSeen,
    bool IsStale,
    int? SearchCount
);
