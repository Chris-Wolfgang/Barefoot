namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// One search of the run in flight: a row of the Admin's Runs page (DESIGN §12).
/// </summary>
/// <param name="SearchId">The search's id.</param>
/// <param name="SearchName">The search's name.</param>
/// <param name="OwnerName">The display name of the person the search belongs to.</param>
/// <param name="Stage">How far the search has come.</param>
/// <param name="Lookups">
/// The room lookups the search wants. A lookup wanted by several searches is made once and counted
/// under each.
/// </param>
/// <param name="RunOrder">The search's run order, 1 (last) to 99 (first).</param>
public sealed record SearchProgressResponse
(
    Guid SearchId,
    string SearchName,
    string OwnerName,
    SearchRunStage Stage,
    WorkProgress Lookups,
    int RunOrder
);
