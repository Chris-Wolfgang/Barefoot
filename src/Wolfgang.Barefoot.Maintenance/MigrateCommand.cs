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
/// The verb <c>bf-maint migrate</c> (docs/MIGRATE.md). Alone it applies every pending migration;
/// with <c>--to</c> it moves up or down to the migration named.
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
        var options = new Options();

        var command = new Command("migrate", "Moves the schema to a migration: by default up to the latest this build ships.")
        {
            options.Status,
            options.Script,
            options.Output,
            options.To,
            options.ConfirmDataLoss,
        };

        command.Validators.Add
        (
            result =>
            {
                ValidateScript(result, options);
                ValidateMove(result, options);
            }
        );

        command.SetAction
        (
            (parsed, cancellationToken) =>
            {
                if (parsed.GetValue(options.Script))
                {
                    return ScriptAsync
                    (
                        parsed.InvocationConfiguration,
                        offlineContext,
                        parsed.GetRequiredValue(options.Output),
                        cancellationToken
                    );
                }

                var target = parsed.GetValue(options.To);
                var confirmed = parsed.GetValue(options.ConfirmDataLoss);

                return OnDatabaseAsync
                (
                    parsed.InvocationConfiguration,
                    configuration,
                    openContext,
                    parsed.GetValue(options.Status)
                        ? StatusAsync
                        : (context, streams, cancellation) => MoveAsync(context, streams, target, confirmed, cancellation),
                    cancellationToken
                );
            }
        );

        return command;
    }



    private static void ValidateScript(CommandResult result, Options options)
    {
        var scripted = result.GetValue(options.Script);
        var hasOutput = !string.IsNullOrWhiteSpace(result.GetValue(options.Output));

        if (scripted && !hasOutput)
        {
            result.AddError("--script needs --output <file>.");
        }

        if (!scripted && hasOutput)
        {
            result.AddError("--output is only used with --script.");
        }

        if (scripted && result.GetValue(options.Status))
        {
            result.AddError("--status and --script cannot be used together.");
        }
    }



    private static void ValidateMove(CommandResult result, Options options)
    {
        var reportsOnly = result.GetValue(options.Status);
        var scripted = result.GetValue(options.Script);
        var hasTarget = result.GetValue(options.To) is not null;

        if (hasTarget && reportsOnly)
        {
            result.AddError("--to is not used with --status.");
        }

        if (hasTarget && scripted)
        {
            result.AddError("--script does not take --to yet: it always writes every migration.");
        }

        if (result.GetValue(options.ConfirmDataLoss) && (reportsOnly || scripted))
        {
            result.AddError("--confirm-data-loss is only used when migrations are applied or rolled back.");
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
            return await RefuseAsync
            (
                streams,
                "No connection string. Set the environment variable ConnectionStrings__Barefoot, or the user secret ConnectionStrings:Barefoot.",
                ExitCode.Usage
            ).ConfigureAwait(false);
        }

        BarefootDbContext context;
        try
        {
            context = openContext(connectionString);
        }
        catch (ArgumentException)
        {
            // The exception's message is not written: it can quote part of the connection string.
            return await RefuseAsync
            (
                streams,
                "The connection string ConnectionStrings:Barefoot is not a valid connection string.",
                ExitCode.Usage
            ).ConfigureAwait(false);
        }

        await using (context.ConfigureAwait(false))
        {
            var canConnect = await context
                .Database
                .CanConnectAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!canConnect)
            {
                return await RefuseAsync
                (
                    streams,
                    "The database could not be reached. bf-maint never creates a database: check that it exists, and that the firewall rule allows this machine.",
                    ExitCode.DatabaseUnavailable
                ).ConfigureAwait(false);
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



    /// <summary>
    /// Moves the database to the migration <paramref name="to"/> names, up or down, or to the latest
    /// when it is <see langword="null"/>. A move down that would lose data needs
    /// <paramref name="confirmed"/>.
    /// </summary>
    private static async Task<int> MoveAsync
    (
        BarefootDbContext context,
        InvocationConfiguration streams,
        string? to,
        bool confirmed,
        CancellationToken cancellationToken
    )
    {
        var before = await SchemaProbe
            .ReadAsync(context, cancellationToken)
            .ConfigureAwait(false);

        if (before.State == SchemaState.NewerSchema)
        {
            return await RefuseAsync
            (
                streams,
                $"The database is newer than this build: it holds {Listed(before.Unknown)}. Nothing was applied. Deploy the matching build.",
                ExitCode.Refused
            ).ConfigureAwait(false);
        }

        IReadOnlyList<string> shipped = [.. context.Database.GetMigrations()];

        var target = MigrationTarget.Resolve(shipped, to ?? before.Expected ?? MigrationTarget.EmptySchema);
        if (target.Id is null)
        {
            var problem = target.Kind == MigrationTargetKind.Ambiguous
                ? $"'{to}' matches more than one migration: {Listed(target.Candidates)}. Give more of the id or the name."
                : $"No migration matches '{to}'. This build ships: {Listed(shipped)}.";

            return await RefuseAsync(streams, problem, ExitCode.Usage).ConfigureAwait(false);
        }

        var applied = await context
            .Database
            .GetAppliedMigrationsAsync(cancellationToken)
            .ConfigureAwait(false);

        var plan = MigrationPlan.Make(shipped, applied, target.Id);

        var losses = confirmed ? [] : Losses(context, plan.Revert);
        if (losses.Count > 0)
        {
            return await RefuseAsync(streams, DataLossRefusal(target.Id, losses), ExitCode.DataLossNotConfirmed).ConfigureAwait(false);
        }

        return await CarryOutAsync(context, streams, plan, target.Id, cancellationToken).ConfigureAwait(false);
    }



    /// <summary>Runs <paramref name="plan"/> when it changes something, then reports what was done.</summary>
    private static async Task<int> CarryOutAsync
    (
        BarefootDbContext context,
        InvocationConfiguration streams,
        MigrationPlan plan,
        string targetId,
        CancellationToken cancellationToken
    )
    {
        if (!string.Equals(plan.Direction, MigrationPlan.Nowhere, StringComparison.Ordinal))
        {
            await context
                .GetService<IMigrator>()
                .MigrateAsync(targetId, cancellationToken)
                .ConfigureAwait(false);
        }

        var after = await SchemaProbe
            .ReadAsync(context, cancellationToken)
            .ConfigureAwait(false);

        var report = string.Join
        (
            '\n',
            $"direction: {plan.Direction}",
            $"applied: {Listed(plan.Apply)}",
            $"reverted: {Listed(plan.Revert)}",
            $"current: {after.Current ?? None}"
        );

        await streams
            .Output
            .WriteLineAsync(report)
            .ConfigureAwait(false);

        return (int)ExitCode.Ok;
    }



    /// <summary>The steps that would lose data when <paramref name="reverted"/> are rolled back, in that order.</summary>
    private static List<DataLossStep> Losses(BarefootDbContext context, IReadOnlyList<string> reverted)
    {
        var assembly = context.GetService<IMigrationsAssembly>();

        // A context that has just read its migration history has a provider, so the name is not null.
        var provider = context.Database.ProviderName!;

        return
        [
            .. reverted.SelectMany
            (
                id => DataLossScreen.Find(id, assembly.CreateMigration(assembly.Migrations[id], provider).DownOperations)
            ),
        ];
    }



    private static string DataLossRefusal(string targetId, List<DataLossStep> losses)
    {
        return string.Join
        (
            '\n',
            [
                $"Moving down to {targetId} would lose data:",
                .. losses.Select(step => $"  {step.MigrationId}: {step.Description}"),
                "Nothing was changed. Give --confirm-data-loss to go ahead.",
            ]
        );
    }



    /// <summary>Writes <paramref name="sentence"/> to the error stream and gives <paramref name="exitCode"/> as a number.</summary>
    private static async Task<int> RefuseAsync(InvocationConfiguration streams, string sentence, ExitCode exitCode)
    {
        await streams
            .Error
            .WriteLineAsync(sentence)
            .ConfigureAwait(false);

        return (int)exitCode;
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

        return status.State switch
        {
            SchemaState.NewerSchema => await RefuseAsync
            (
                streams,
                "The database is newer than this build. Deploy the matching build.",
                ExitCode.Refused
            ).ConfigureAwait(false),

            SchemaState.MissingMigration => await RefuseAsync
            (
                streams,
                $"Missing migration: {Listed(status.Pending)}. Run `bf-maint migrate`.",
                ExitCode.MissingMigration
            ).ConfigureAwait(false),

            _ => (int)ExitCode.Ok,
        };
    }



    private static string Listed(IReadOnlyList<string> ids)
    {
        return ids.Count == 0 ? None : string.Join(", ", ids);
    }



    /// <summary>The options of the verb.</summary>
    private sealed class Options
    {
        public Option<bool> Status { get; } = new("--status")
        {
            Description = "Report the schema version of the database against the one this build expects. Changes nothing.",
        };

        public Option<bool> Script { get; } = new("--script")
        {
            Description = "Write the SQL of every migration this build ships to --output instead of applying it. Opens no connection.",
        };

        public Option<string> Output { get; } = new("--output")
        {
            Description = "With --script: the file the SQL is written to. An existing file is replaced.",
        };

        public Option<string> To { get; } = new("--to")
        {
            Description = "The migration to move to, up or down: its id, its name, a prefix of either, or 0 for an empty schema. Default: the latest.",
        };

        public Option<bool> ConfirmDataLoss { get; } = new("--confirm-data-loss")
        {
            Description = "Go ahead with a move down that would lose data.",
        };
    }
}
