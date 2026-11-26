using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;
using Wolfgang.Barefoot.Data.Schema;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// The verb <c>bf-maint migrate</c> (docs/MIGRATE.md). Alone it applies every pending migration.
/// With <c>--status</c> it reads the schema version of the database against the one this build
/// expects and changes nothing. With <c>--script</c> it writes the SQL of the migrations to a file
/// and opens no connection.
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
    /// <param name="offlineContext">Makes a context that names no database, for <c>--script</c>.</param>
    public static Command Create
    (
        IConfiguration configuration,
        Func<string, BarefootDbContext> openContext,
        Func<BarefootDbContext> offlineContext
    )
    {
        var status = new Option<bool>("--status")
        {
            Description = "Report the schema version of the database against the one this build expects. Changes nothing.",
        };

        var script = new Option<bool>("--script")
        {
            Description = "Write the SQL of every migration this build ships to --output instead of applying it. Opens no connection.",
        };

        var output = new Option<string>("--output")
        {
            Description = "With --script: the file the SQL is written to. An existing file is replaced.",
        };

        var command = new Command("migrate", "Applies every pending migration, up to the latest this build ships.")
        {
            status,
            script,
            output,
        };

        command.Validators.Add(result => Validate(result, status, script, output));

        command.SetAction
        (
            (parsed, cancellationToken) =>
            {
                if (parsed.GetValue(script))
                {
                    return ScriptAsync(parsed.InvocationConfiguration, offlineContext, parsed.GetRequiredValue(output), cancellationToken);
                }

                return OnDatabaseAsync
                (
                    parsed.InvocationConfiguration,
                    configuration,
                    openContext,
                    parsed.GetValue(status) ? StatusAsync : ApplyAsync,
                    cancellationToken
                );
            }
        );

        return command;
    }



    private static void Validate(CommandResult result, Option<bool> status, Option<bool> script, Option<string> output)
    {
        var scripted = result.GetValue(script);
        var hasOutput = !string.IsNullOrWhiteSpace(result.GetValue(output));

        if (scripted && !hasOutput)
        {
            result.AddError("--script needs --output <file>.");
        }

        if (!scripted && hasOutput)
        {
            result.AddError("--output is only used with --script.");
        }

        if (scripted && result.GetValue(status))
        {
            result.AddError("--status and --script cannot be used together.");
        }
    }



    private static async Task<int> ScriptAsync
    (
        InvocationConfiguration streams,
        Func<BarefootDbContext> offlineContext,
        string output,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<string> migrations;
        string sql;

        var context = offlineContext();
        await using (context.ConfigureAwait(false))
        {
            migrations = [.. context.Database.GetMigrations()];
            sql = context
                .GetService<IMigrator>()
                .GenerateScript();
        }

        var path = Path.GetFullPath(output);

        await File
            .WriteAllTextAsync(path, sql, cancellationToken)
            .ConfigureAwait(false);

        await streams
            .Output
            .WriteLineAsync($"script: {path}\nmigrations: {Listed(migrations)}")
            .ConfigureAwait(false);

        return (int)ExitCode.Ok;
    }



    /// <summary>
    /// Opens the configured database and runs <paramref name="work"/> on it. Without a usable
    /// connection string it exits <see cref="ExitCode.Usage"/>; when the database cannot be reached it
    /// exits <see cref="ExitCode.DatabaseUnavailable"/>. In both cases <paramref name="work"/> does not run.
    /// </summary>
    private static async Task<int> OnDatabaseAsync
    (
        InvocationConfiguration streams,
        IConfiguration configuration,
        Func<string, BarefootDbContext> openContext,
        Func<BarefootDbContext, InvocationConfiguration, CancellationToken, Task<int>> work,
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

            return await work(context, streams, cancellationToken).ConfigureAwait(false);
        }
    }



    private static async Task<int> StatusAsync
    (
        BarefootDbContext context,
        InvocationConfiguration streams,
        CancellationToken cancellationToken
    )
    {
        var status = await SchemaProbe
            .ReadAsync(context, cancellationToken)
            .ConfigureAwait(false);

        return await ReportAsync(status, streams).ConfigureAwait(false);
    }



    private static async Task<int> ApplyAsync
    (
        BarefootDbContext context,
        InvocationConfiguration streams,
        CancellationToken cancellationToken
    )
    {
        var before = await SchemaProbe
            .ReadAsync(context, cancellationToken)
            .ConfigureAwait(false);

        if (before.State == SchemaState.NewerSchema)
        {
            await streams
                .Error
                .WriteLineAsync($"The database is newer than this build: it holds {Listed(before.Unknown)}. Nothing was applied. Deploy the matching build.")
                .ConfigureAwait(false);

            return (int)ExitCode.Refused;
        }

        if (before.State == SchemaState.MissingMigration)
        {
            await context
                .Database
                .MigrateAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var after = await SchemaProbe
            .ReadAsync(context, cancellationToken)
            .ConfigureAwait(false);

        var report = string.Join
        (
            '\n',
            $"applied: {Listed(before.Pending)}",
            $"current: {after.Current ?? None}"
        );

        await streams
            .Output
            .WriteLineAsync(report)
            .ConfigureAwait(false);

        return (int)ExitCode.Ok;
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
