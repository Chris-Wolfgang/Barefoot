using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// The verb <c>bf-maint grant-roles</c> (docs/MIGRATE.md, ADR 0016). It makes the API's database user
/// and adds it to its roles; on a server that cannot make the user it says so and changes nothing.
/// </summary>
internal static class GrantRolesCommand
{
    /// <summary>
    /// Makes the verb.
    /// </summary>
    /// <param name="configuration">Holds the connection string <see cref="MigrateCommand.ConnectionStringName"/>.</param>
    /// <param name="openContext">Makes a context on the database a connection string names.</param>
    /// <param name="roles">The SQL that makes the user and adds it to its roles.</param>
    public static Command Create
    (
        IConfiguration configuration,
        Func<string, BarefootDbContext> openContext,
        RoleScript roles
    )
    {
        var command = new Command("grant-roles", "Makes the API's database user and adds it to its roles. Running it again changes nothing.");

        command.SetAction
        (
            (parsed, cancellationToken) => MigrateCommand.OnDatabaseAsync
            (
                parsed.InvocationConfiguration,
                configuration,
                openContext,
                (context, streams, cancellation) => GrantAsync(context, streams, roles, cancellation),
                cancellationToken
            )
        );

        return command;
    }



    private static async Task<int> GrantAsync
    (
        BarefootDbContext context,
        InvocationConfiguration streams,
        RoleScript roles,
        CancellationToken cancellationToken
    )
    {
        var granted = await RoleGrant
            .RunAsync(context, roles, cancellationToken)
            .ConfigureAwait(false);

        await streams
            .Output
            .WriteLineAsync(granted ? "roles: granted" : $"roles: skipped ({roles.WhyNot})")
            .ConfigureAwait(false);

        return (int)ExitCode.Ok;
    }
}
