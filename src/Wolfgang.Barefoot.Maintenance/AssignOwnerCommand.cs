using System.CommandLine;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Entities;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;
using Wolfgang.Barefoot.Data.Schema;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// The verb <c>bf-maint assign-owner &lt;oid&gt;</c> (docs/MIGRATE.md, ADR 0004 §8, ADR 0016). It moves
/// everything the seeded "Unassigned" user owns to the user named, who must have signed in once.
/// </summary>
internal static class AssignOwnerCommand
{
    /// <summary>
    /// Makes the verb.
    /// </summary>
    /// <param name="configuration">Holds the connection string <see cref="MigrateCommand.ConnectionStringName"/>.</param>
    /// <param name="openContext">Makes a context on the database a connection string names.</param>
    public static Command Create(IConfiguration configuration, Func<string, BarefootDbContext> openContext)
    {
        var oid = new Argument<string>("oid")
        {
            Description = "The object id of the user who gets the rows: a GUID.",
        };

        var command = new Command("assign-owner", "Moves everything the \"Unassigned\" user owns to the user named. Run it once.")
        {
            oid,
        };

        command.Validators.Add
        (
            result =>
            {
                var text = result.GetValue(oid);
                if (!Guid.TryParse(text, out _))
                {
                    result.AddError($"'{text}' is not an object id. Give a GUID.");
                }
            }
        );

        command.SetAction
        (
            (parsed, cancellationToken) =>
            {
                var target = Guid.Parse(parsed.GetRequiredValue(oid));

                return MigrateCommand.OnDatabaseAsync
                (
                    parsed.InvocationConfiguration,
                    configuration,
                    openContext,
                    (context, streams, cancellation) => AssignAsync(context, streams, target, cancellation),
                    cancellationToken
                );
            }
        );

        return command;
    }



    private static async Task<int> AssignAsync
    (
        BarefootDbContext context,
        InvocationConfiguration streams,
        Guid target,
        CancellationToken cancellationToken
    )
    {
        var status = await SchemaProbe
            .ReadAsync(context, cancellationToken)
            .ConfigureAwait(false);

        var unfit = await MigrateCommand.RefuseUnlessUpToDateAsync(status, streams).ConfigureAwait(false);
        if (unfit is not null)
        {
            return unfit.Value;
        }

        var problem = await ProblemAsync(context, target, cancellationToken).ConfigureAwait(false);
        if (problem is not null)
        {
            return await MigrateCommand.RefuseAsync(streams, problem, ExitCode.Refused).ConfigureAwait(false);
        }

        var moved = await OwnerMove
            .RunAsync(context, target, cancellationToken)
            .ConfigureAwait(false);

        var report = string.Join
        (
            '\n',
            string.Create(CultureInfo.InvariantCulture, $"searches: {moved.Searches}"),
            string.Create(CultureInfo.InvariantCulture, $"recipients: {moved.Recipients}"),
            string.Create(CultureInfo.InvariantCulture, $"issues: {moved.Issues}"),
            string.Create(CultureInfo.InvariantCulture, $"deliveries: {moved.Deliveries}")
        );

        await streams
            .Output
            .WriteLineAsync(report)
            .ConfigureAwait(false);

        return (int)ExitCode.Ok;
    }



    /// <summary>
    /// Why nothing can move to <paramref name="target"/>: it is the "Unassigned" user, it has never
    /// signed in, or it already has a search or a recipient that would be there twice. Null when the
    /// move can go ahead.
    /// </summary>
    private static async Task<string?> ProblemAsync(BarefootDbContext context, Guid target, CancellationToken cancellationToken)
    {
        if (target == AppUser.UnassignedOid)
        {
            return "That is the \"Unassigned\" user itself. Nothing was moved.";
        }

        var known = await context
            .AppUsers
            .AnyAsync(user => user.Oid == target, cancellationToken)
            .ConfigureAwait(false);

        if (!known)
        {
            return $"No user {target} has signed in. Nothing was moved. The user has to sign in once first.";
        }

        var collisions = await OwnerMove
            .CollisionsAsync(context, target, cancellationToken)
            .ConfigureAwait(false);

        return collisions.Count == 0
            ? null
            : $"User {target} already has what would be moved:\n  {string.Join("\n  ", collisions)}\nNothing was moved. Rename or delete one of each pair, then run it again.";
    }
}
