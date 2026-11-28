namespace Wolfgang.Barefoot.Business.Catalog;

/// <summary>
/// Tells whether a room-vocabulary value has gone stale: not seen in the results of the last runs
/// (ADR 0011, <c>vocab.stale_after_runs</c>; Barefoot#93). A stale value is greyed in the search
/// editor and never deleted.
/// </summary>
public static class VocabularyStaleness
{
    /// <summary>
    /// How many of the runs started after the value was last seen, and whether that is as many as
    /// make it stale.
    /// </summary>
    /// <param name="lastSeenUtc">When a result last carried the value.</param>
    /// <param name="runStartsUtc">
    /// When each run that counts started, in any order. A run counts when it read results: the
    /// caller leaves out the runs that failed outright.
    /// </param>
    /// <param name="staleAfterRunCount">How many runs without the value make it stale. At least 1.</param>
    public static VocabularyAge Of
    (
        DateTimeOffset lastSeenUtc,
        IEnumerable<DateTimeOffset> runStartsUtc,
        int staleAfterRunCount
    )
    {
        ArgumentNullException.ThrowIfNull(runStartsUtc);
        ArgumentOutOfRangeException.ThrowIfLessThan(staleAfterRunCount, 1);

        var runsSinceSeen = runStartsUtc.Count(started => started > lastSeenUtc);

        return new VocabularyAge(runsSinceSeen, runsSinceSeen >= staleAfterRunCount);
    }
}
