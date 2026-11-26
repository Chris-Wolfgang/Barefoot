namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// What moving a database to a target migration takes: which migrations to apply and which to roll
/// back. <see cref="Make"/> works it out from ids alone and reads no database.
/// </summary>
internal sealed class MigrationPlan
{
    /// <summary>The direction of a plan that rolls a migration back.</summary>
    public const string Down = "down";

    /// <summary>The direction of a plan that applies migrations and rolls none back.</summary>
    public const string Up = "up";

    /// <summary>The direction of a plan that changes nothing.</summary>
    public const string Nowhere = "none";



    private MigrationPlan(IReadOnlyList<string> apply, IReadOnlyList<string> revert)
    {
        Apply = apply;
        Revert = revert;
    }



    /// <summary>The ids of the migrations to apply, oldest first.</summary>
    public IReadOnlyList<string> Apply { get; }



    /// <summary>The ids of the migrations to roll back, newest first: the order they are rolled back in.</summary>
    public IReadOnlyList<string> Revert { get; }



    /// <summary>
    /// <see cref="Down"/> when a migration is rolled back, whatever else is applied; otherwise
    /// <see cref="Up"/> when one is applied; otherwise <see cref="Nowhere"/>.
    /// </summary>
    public string Direction
    {
        get
        {
            if (Revert.Count > 0)
            {
                return Down;
            }

            return Apply.Count > 0 ? Up : Nowhere;
        }
    }



    /// <summary>
    /// Finds the gaps in a history: the shipped migrations that are not applied although a later
    /// shipped one is. The tool never leaves one, so a gap means the history was changed by hand.
    /// Case is ignored.
    /// </summary>
    /// <param name="shipped">The ids of the migrations this build ships.</param>
    /// <param name="applied">The ids of the migrations the database holds. Ids this build does not ship are ignored.</param>
    /// <returns>The ids that are missing, oldest first; none when the history has no gap.</returns>
    public static IReadOnlyList<string> Gaps(IEnumerable<string> shipped, IEnumerable<string> applied)
    {
        ArgumentNullException.ThrowIfNull(shipped);
        ArgumentNullException.ThrowIfNull(applied);

        var ids = shipped
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var held = applied.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newest = ids.FindLastIndex(held.Contains);

        return
        [
            .. ids
                .Take(newest + 1)
                .Where(id => !held.Contains(id)),
        ];
    }



    /// <summary>
    /// Plans the move of a database that holds <paramref name="applied"/> to <paramref name="targetId"/>.
    /// Every shipped migration up to the target that is not applied is applied; every applied one
    /// after the target is rolled back. Case is ignored.
    /// </summary>
    /// <param name="shipped">The ids of the migrations this build ships.</param>
    /// <param name="applied">The ids of the migrations the database holds. Ids this build does not ship are ignored.</param>
    /// <param name="targetId">The id of a shipped migration, or <see cref="MigrationTarget.EmptySchema"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="targetId"/> is not shipped and is not the empty schema.</exception>
    public static MigrationPlan Make(IEnumerable<string> shipped, IEnumerable<string> applied, string targetId)
    {
        ArgumentNullException.ThrowIfNull(shipped);
        ArgumentNullException.ThrowIfNull(applied);
        ArgumentNullException.ThrowIfNull(targetId);

        var ids = shipped
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var kept = 0;
        if (!string.Equals(targetId, MigrationTarget.EmptySchema, StringComparison.Ordinal))
        {
            kept = ids.FindIndex(id => string.Equals(id, targetId, StringComparison.OrdinalIgnoreCase)) + 1;
            if (kept == 0)
            {
                throw new ArgumentException($"This build ships no migration '{targetId}'.", nameof(targetId));
            }
        }

        var held = applied.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new MigrationPlan
        (
            [.. ids.Take(kept).Where(id => !held.Contains(id))],
            [.. ids.Skip(kept).Where(held.Contains).Reverse()]
        );
    }
}
