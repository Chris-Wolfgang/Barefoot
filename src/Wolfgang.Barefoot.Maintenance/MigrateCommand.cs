using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;
using Wolfgang.Barefoot.Data.Schema;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// The verb <c>bf-maint migrate</c> (docs/MIGRATE.md). So far it reports: <c>--status</c> reads the
/// schema version of the database against the one this build expects and changes nothing.
/// </summary>
internal static class MigrateCommand
{
    /// <summary>
    /// The name of the connection string: <c>ConnectionStrings:Barefoot</c> in configuration and
    /// <c>ConnectionStrings__Barefoot</c> as an environment variable (ADR 0015).
    /// </summary>
    public const string ConnectionStringName = "Barefoot";

    private const string None = "none";



    /// <summary>
    /// Makes the verb.
    /// </summary>
    /// <param name="configuration">Holds the connection string <see cref="ConnectionStringName"/>.</param>
    /// <param name="openContext">Makes a context on the database a connection string names.</param>
    public static Command Create(IConfiguration configuration, Func<string, BarefootDbContext> openContext)
    {
        var status = new Option<bool>("--status")
        {
            Description = "Report the schema version of the database against the one this build expects. Changes nothing.",
        };

        var command = new Command("migrate", "Reports the schema's version. Applying and scripting migrations are not built yet.")
        {
            status,
        };

        command.SetAction
        (
            (parsed, cancellationToken) => parsed.GetValue(status)
                ? StatusAsync(parsed.InvocationConfiguration, configuration, openContext, cancellationToken)
                : NotBuiltAsync(parsed.InvocationConfiguration.Error)
        );

        return command;
    }



    private static async Task<int> NotBuiltAsync(TextWriter error)
    {
        await error
            .WriteLineAsync("bf-maint migrate does not apply migrations yet. Only `bf-maint migrate --status` is built.")
            .ConfigureAwait(false);

        return (int)ExitCode.Usage;
    }



    private static async Task<int> StatusAsync
    (
        InvocationConfiguration streams,
        IConfiguration configuration,
        Func<string, BarefootDbContext> openContext,
        CancellationToken cancellationToken
    )
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            await streams
                .Error
                .WriteLineAsync("No connection string. Set the environment variable ConnectionStrings__Barefoot, or the user secret ConnectionStrings:Barefoot.")
                .ConfigureAwait(false);

            return (int)ExitCode.Usage;
        }

        BarefootDbContext context;
        try
        {
            context = openContext(connectionString);
        }
        catch (ArgumentException)
        {
            // The exception's message is not written: it can quote part of the connection string.
            await streams
                .Error
                .WriteLineAsync("The connection string ConnectionStrings:Barefoot is not a valid connection string.")
                .ConfigureAwait(false);

            return (int)ExitCode.Usage;
        }

        await using (context.ConfigureAwait(false))
        {
            var canConnect = await context
                .Database
                .CanConnectAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!canConnect)
            {
                await streams
                    .Error
                    .WriteLineAsync("The database could not be reached. bf-maint never creates a database: check that it exists, and that the firewall rule allows this machine.")
                    .ConfigureAwait(false);

                return (int)ExitCode.DatabaseUnavailable;
            }

            var status = await SchemaProbe
                .ReadAsync(context, cancellationToken)
                .ConfigureAwait(false);

            return await ReportAsync(status, streams).ConfigureAwait(false);
        }
    }



    private static async Task<int> ReportAsync(SchemaStatus status, InvocationConfiguration streams)
    {
        var report = string.Join
        (
            '\n',
            $"current: {status.Current ?? None}",
            $"expected: {status.Expected ?? None}",
            $"state: {status.State}",
            $"pending: {Listed(status.Pending)}",
            $"unknown: {Listed(status.Unknown)}"
        );

        await streams
            .Output
            .WriteLineAsync(report)
            .ConfigureAwait(false);

        switch (status.State)
        {
            case SchemaState.NewerSchema:
                await streams
                    .Error
                    .WriteLineAsync("The database is newer than this build. Deploy the matching build.")
                    .ConfigureAwait(false);

                return (int)ExitCode.Refused;

            case SchemaState.MissingMigration:
                await streams
                    .Error
                    .WriteLineAsync($"Missing migration: {Listed(status.Pending)}. Run `bf-maint migrate`.")
                    .ConfigureAwait(false);

                return (int)ExitCode.MissingMigration;

            default:
                return (int)ExitCode.Ok;
        }
    }



    private static string Listed(IReadOnlyList<string> ids)
    {
        return ids.Count == 0 ? None : string.Join(", ", ids);
    }
}
