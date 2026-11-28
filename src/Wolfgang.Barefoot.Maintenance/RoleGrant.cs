using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// Gives the API its database user and roles by running a <see cref="RoleScript"/> (ADR 0016).
/// </summary>
internal static class RoleGrant
{
    /// <summary>
    /// Asks the server whether it can make the user and, when it can, runs the statements of
    /// <paramref name="script"/> in order.
    /// </summary>
    /// <param name="context">A context on the database.</param>
    /// <param name="script">The SQL for the kind of server <paramref name="context"/> is on.</param>
    /// <param name="cancellationToken">Cancels the work between two statements.</param>
    /// <returns>True when the statements were run; false when the server cannot make the user and nothing was run.</returns>
    public static async Task<bool> RunAsync(BarefootDbContext context, RoleScript script, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(script);

        var answer = await context
            .Database
            .SqlQueryRaw<int>(script.CanGrantQuery)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (answer[0] == 0)
        {
            return false;
        }

        foreach (var statement in script.Statements)
        {
            await context
                .Database
                .ExecuteSqlRawAsync(statement, cancellationToken)
                .ConfigureAwait(false);
        }

        return true;
    }
}
