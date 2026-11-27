using System.CommandLine;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;
using Wolfgang.Barefoot.Data.Schema;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// The verb <c>bf-maint verify-schema</c> (docs/MIGRATE.md, ADR 0016). It reads the tables, columns,
/// keys, indexes and foreign keys the database has, compares them with the model, and changes
/// nothing.
/// </summary>
internal static class VerifySchemaCommand
{
    /// <summary>
    /// Makes the verb.
    /// </summary>
    /// <param name="configuration">Holds the connection string <see cref="MigrateCommand.ConnectionStringName"/>.</param>
    /// <param name="openContext">Makes a context on the database a connection string names.</param>
    /// <param name="catalogQuery">
    /// The query that reads the database's schema as lines <c>item|detail</c> in a column named
    /// <c>Value</c> (<see cref="SchemaLines"/>).
    /// </param>
    public static Command Create
    (
        IConfiguration configuration,
        Func<string, BarefootDbContext> openContext,
        string catalogQuery
    )
    {
        var command = new Command("verify-schema", "Compares the tables, columns, keys and indexes of the database with what this build expects. Changes nothing.");

        command.SetAction
        (
            (parsed, cancellationToken) => MigrateCommand.OnDatabaseAsync
            (
                parsed.InvocationConfiguration,
                configuration,
                openContext,
                (context, streams, cancellation) => VerifyAsync(context, streams, catalogQuery, cancellation),
                cancellationToken
            )
        );

        return command;
    }



    private static async Task<int> VerifyAsync
    (
        BarefootDbContext context,
        InvocationConfiguration streams,
        string catalogQuery,
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

        var actual = await context
            .Database
            .SqlQueryRaw<string>(catalogQuery)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var differences = SchemaDrift.Compare(SchemaLines.Expected(context), actual.Select(SchemaLines.Normalized));

        foreach (var difference in differences)
        {
            await streams
                .Error
                .WriteLineAsync(difference)
                .ConfigureAwait(false);
        }

        await streams
            .Output
            .WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"differences: {differences.Count}"))
            .ConfigureAwait(false);

        return (int)(differences.Count == 0 ? ExitCode.Ok : ExitCode.Failed);
    }
}
