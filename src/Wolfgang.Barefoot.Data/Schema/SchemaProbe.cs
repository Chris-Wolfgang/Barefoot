using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Barefoot.Data.Schema;

/// <summary>
/// Reads the schema version of a database and compares it with the migrations this build ships. It is
/// the one code path for that question (ADR 0001 §4): <c>bf-maint migrate</c> and
/// <c>GET /system/schema</c> both ask it, so they cannot disagree.
/// </summary>
public static class SchemaProbe
{
    /// <summary>
    /// Reads the migrations the database of <paramref name="context"/> holds and compares them with
    /// the ones this build ships. It changes nothing. A database with no migration history holds none.
    /// </summary>
    /// <param name="context">A context on the database to read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static async Task<SchemaStatus> ReadAsync(BarefootDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var applied = await context
            .Database
            .GetAppliedMigrationsAsync(cancellationToken)
            .ConfigureAwait(false);

        return Compare(context.Database.GetMigrations(), applied);
    }



    /// <summary>
    /// Compares the migrations a build ships with the ones a database holds. Ids are compared as EF
    /// Core compares them, without regard to case, and the order they are given in does not matter.
    /// </summary>
    /// <param name="shipped">The ids of the migrations the build ships.</param>
    /// <param name="applied">The ids of the migrations the database holds.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static SchemaStatus Compare(IEnumerable<string> shipped, IEnumerable<string> applied)
    {
        ArgumentNullException.ThrowIfNull(shipped);
        ArgumentNullException.ThrowIfNull(applied);

        var shippedIds = Sorted(shipped);
        var appliedIds = Sorted(applied);

        return new SchemaStatus
        (
            current: appliedIds.LastOrDefault(),
            expected: shippedIds.LastOrDefault(),
            pending: shippedIds.Except(appliedIds, StringComparer.OrdinalIgnoreCase).ToList(),
            unknown: appliedIds.Except(shippedIds, StringComparer.OrdinalIgnoreCase).ToList()
        );
    }



    private static List<string> Sorted(IEnumerable<string> ids)
    {
        return ids
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
