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
/// with <c>--to</c> it moves up or down to the migration named. A move that ends on the latest
/// migration also gives the API its database user and roles (ADR 0016).
/// With <c>--status</c> it reads the schema version of the database against the one this build
/// expects and changes nothing. With <c>--script</c> it writes the SQL of a move to a file, by default
/// from an empty schema to the latest migration, and opens no connection.
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
    /// <param name="roles">The SQL that makes the API's database user and adds it to its roles.</param>
    public static Command Create
    (
        IConfiguration configuration,
        Func<string, BarefootDbContext> openContext,
        Func<BarefootDbContext> offlineContext,
        RoleScript roles
    )
    {
        var options = new Options();

        var command = new Command("migrate", "Moves the schema to a migration: by default up to the latest this build ships.")
        {
            options.Status,
            options.Script,
            options.Output,
            options.From,
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
                var target = parsed.GetValue(options.To);
                var confirmed = parsed.GetValue(options.ConfirmDataLoss);

                if (parsed.GetValue(options.Script))
                {
                    var request = new ScriptRequest
                    (
                        parsed.GetRequiredValue(options.Output),
                        parsed.GetValue(options.From),
                        target,
                        confirmed
                    );

                    return ScriptAsync(parsed.InvocationConfiguration, offlineContext, request, cancellationToken);
                }

                return OnDatabaseAsync
                (
                    parsed.InvocationConfiguration,
                    configuration,
                    openContext,
                    parsed.GetValue(options.Status)
                        ? StatusAsync
                        : (context, streams, cancellation) => MoveAsync(context, streams, new MoveRequest(target, confirmed, roles), cancellation),
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

        if (reportsOnly && result.GetValue(options.To) is not null)
        {
            result.AddError("--to is not used with --status.");
        }

        if (reportsOnly && result.GetValue(options.ConfirmDataLoss))
        {
            result.AddError("--confirm-data-loss is not used with --status.");
        }

        if (!result.GetValue(options.Script) && result.GetValue(options.From) is not null)
        {
            result.AddError("--from is only used with --script.");
        }
    }



    /// <summary>
    /// Writes the SQL that moves a database from one migration to another to a file. It opens no
    /// connection: the range is taken from the request, not from a database.
    /// </summary>
    private static async Task<int> ScriptAsync
    (
        InvocationConfiguration streams,
        Func<BarefootDbContext> offlineContext,
        ScriptRequest request,
        CancellationToken cancellationToken
    )
    {
        MigrationPlan plan;
        string sql;

        var context = offlineContext();
        await using (context.ConfigureAwait(false))
        {
            IReadOnlyList<string> shipped = [.. context.Database.GetMigrations()];
            var latest = shipped
                .DefaultIfEmpty(MigrationTarget.EmptySchema)
                .Last();

            var from = MigrationTarget.Resolve(shipped, request.From ?? MigrationTarget.EmptySchema);
            if (from.Id is null)
            {
                return await RefuseAsync(streams, TargetProblem(request.From, from, shipped), ExitCode.Usage).ConfigureAwait(false);
            }

            var to = MigrationTarget.Resolve(shipped, request.To ?? latest);
            if (to.Id is null)
            {
                return await RefuseAsync(streams, TargetProblem(request.To, to, shipped), ExitCode.Usage).ConfigureAwait(false);
            }

            // What a database at `from` holds is every migration a move from nothing to `from` applies.
            plan = MigrationPlan.Make(shipped, MigrationPlan.Make(shipped, [], from.Id).Apply, to.Id);

            var losses = Losses(context, plan.Revert);
            if (losses.Count > 0 && !request.Confirmed)
            {
                return await RefuseAsync
                (
                    streams,
                    DataLossRefusal(to.Id, losses, "Nothing was written."),
                    ExitCode.DataLossNotConfirmed
                ).ConfigureAwait(false);
            }

            sql = context
                .GetService<IMigrator>()
                .GenerateScript(from.Id, to.Id);

            if (plan.Revert.Count > 0)
            {
                sql = DowngradeHeader(from.Id, to.Id, losses) + sql;
            }
        }

        return await WriteScriptAsync(streams, request.Output, sql, plan, cancellationToken).ConfigureAwait(false);
    }



    private static async Task<int> WriteScriptAsync
    (
        InvocationConfiguration streams,
        string output,
        string sql,
        MigrationPlan plan,
        CancellationToken cancellationToken
    )
    {
        var path = Path.GetFullPath(output);

        await File
            .WriteAllTextAsync(path, sql, cancellationToken)
            .ConfigureAwait(false);

        var migrations = plan.Revert.Count > 0 ? plan.Revert : plan.Apply;

        await streams
            .Output
            .WriteLineAsync($"script: {path}\ndirection: {plan.Direction}\nmigrations: {Listed(migrations)}")
            .ConfigureAwait(false);

        return (int)ExitCode.Ok;
    }



    /// <summary>
    /// The comment a downgrade script starts with: the range, and each step that loses data
    /// (ADR 0001 §4).
    /// </summary>
    private static string DowngradeHeader(string fromId, string toId, List<DataLossStep> losses)
    {
        IEnumerable<string> steps = losses.Count == 0
            ? ["-- No step loses data."]
            : ["-- Steps that lose data:", .. losses.Select(step => $"--   {step.MigrationId}: {step.Description}")];

        return string.Join
        (
            '\n',
            [
                $"-- Downgrade from {fromId} to {toId}.",
                .. steps,
                string.Empty,
                string.Empty,
            ]
        );
    }



    /// <summary>
    /// Opens the configured database and runs <paramref name="work"/> on it. Without a usable
    /// connection string it exits <see cref="ExitCode.Usage"/>; when the database cannot be reached it
    /// exits <see cref="ExitCode.DatabaseUnavailable"/>. In both cases <paramref name="work"/> does not run.
    /// </summary>
    /// <summary>
    /// Opens the database the configuration names and runs <paramref name="work"/> on it. Without a
    /// usable connection string it exits <see cref="ExitCode.Usage"/>; when the database cannot be
    /// reached, <see cref="ExitCode.DatabaseUnavailable"/>. In both cases <paramref name="work"/> is not run.
    /// </summary>
    internal static async Task<int> OnDatabaseAsync
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
    /// Moves the database to the migration <paramref name="request"/> names, up or down, or to the
    /// latest when it names none. A move down that would lose data needs the request's confirmation.
    /// </summary>
    private static async Task<int> MoveAsync
    (
        BarefootDbContext context,
        InvocationConfiguration streams,
        MoveRequest request,
        CancellationToken cancellationToken
    )
    {
        var to = request.To;

        var before = await SchemaProbe
            .ReadAsync(context, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<string> shipped = [.. context.Database.GetMigrations()];

        IReadOnlyList<string> applied =
        [
            .. await context
                .Database
                .GetAppliedMigrationsAsync(cancellationToken)
                .ConfigureAwait(false),
        ];

        var unfit = HistoryProblem(before, MigrationPlan.Gaps(shipped, applied));
        if (unfit is not null)
        {
            return await RefuseAsync(streams, unfit, ExitCode.Refused).ConfigureAwait(false);
        }

        var target = MigrationTarget.Resolve(shipped, to ?? before.Expected ?? MigrationTarget.EmptySchema);
        if (target.Id is null)
        {
            return await RefuseAsync(streams, TargetProblem(to, target, shipped), ExitCode.Usage).ConfigureAwait(false);
        }

        var plan = MigrationPlan.Make(shipped, applied, target.Id);

        var losses = request.Confirmed ? [] : Losses(context, plan.Revert);
        if (losses.Count > 0)
        {
            return await RefuseAsync
            (
                streams,
                DataLossRefusal(target.Id, losses, "Nothing was changed."),
                ExitCode.DataLossNotConfirmed
            ).ConfigureAwait(false);
        }

        return await CarryOutAsync(context, streams, new Move(plan, target.Id, request.Roles), cancellationToken).ConfigureAwait(false);
    }



    /// <summary>
    /// Why the tool will not move this database, whatever the target: it is newer than this build, or
    /// its history has a gap. Null when it can be moved.
    /// </summary>
    private static string? HistoryProblem(SchemaStatus before, IReadOnlyList<string> gaps)
    {
        if (before.State == SchemaState.NewerSchema)
        {
            return $"The database is newer than this build: it holds {Listed(before.Unknown)}. Nothing was applied. Deploy the matching build.";
        }

        return gaps.Count == 0
            ? null
            : $"The migration history has a gap: {Listed(gaps)} is not applied and a later migration is. Nothing was applied.";
    }



    /// <summary>
    /// Runs the plan of <paramref name="move"/> when it changes something, grants the roles when the
    /// database ends on the latest migration, then reports what was done.
    /// </summary>
    private static async Task<int> CarryOutAsync
    (
        BarefootDbContext context,
        InvocationConfiguration streams,
        Move move,
        CancellationToken cancellationToken
    )
    {
        var plan = move.Plan;

        if (!string.Equals(plan.Direction, MigrationPlan.Nowhere, StringComparison.Ordinal))
        {
            await context
                .GetService<IMigrator>()
                .MigrateAsync(move.TargetId, cancellationToken)
                .ConfigureAwait(false);
        }

        var after = await SchemaProbe
            .ReadAsync(context, cancellationToken)
            .ConfigureAwait(false);

        var roles = after.State == SchemaState.UpToDate
            ? await GrantRolesCommand.OutcomeAsync(context, move.Roles, cancellationToken).ConfigureAwait(false)
            : "not run";

        var report = string.Join
        (
            '\n',
            $"direction: {plan.Direction}",
            $"applied: {Listed(plan.Apply)}",
            $"reverted: {Listed(plan.Revert)}",
            $"current: {after.Current ?? None}",
            $"roles: {roles}"
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



    /// <summary>Why <paramref name="text"/> does not name one migration.</summary>
    private static string TargetProblem(string? text, MigrationTarget target, IReadOnlyList<string> shipped)
    {
        return target.Kind == MigrationTargetKind.Ambiguous
            ? $"'{text}' matches more than one migration: {Listed(target.Candidates)}. Give more of the id or the name."
            : $"No migration matches '{text}'. This build ships: {Listed(shipped)}.";
    }



    private static string DataLossRefusal(string targetId, List<DataLossStep> losses, string outcome)
    {
        return string.Join
        (
            '\n',
            [
                $"Moving down to {targetId} would lose data:",
                .. losses.Select(step => $"  {step.MigrationId}: {step.Description}"),
                $"{outcome} Give --confirm-data-loss to go ahead.",
            ]
        );
    }



    /// <summary>Writes <paramref name="sentence"/> to the error stream and gives <paramref name="exitCode"/> as a number.</summary>
    internal static async Task<int> RefuseAsync(InvocationConfiguration streams, string sentence, ExitCode exitCode)
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

        return await RefuseUnlessUpToDateAsync(status, streams).ConfigureAwait(false) ?? (int)ExitCode.Ok;
    }



    /// <summary>
    /// Says on stderr why a schema that is not the one this build expects cannot be worked on, and
    /// gives the exit code: <see cref="ExitCode.Refused"/> when it is newer,
    /// <see cref="ExitCode.MissingMigration"/> when it is behind. Null, and nothing written, when it
    /// is up to date.
    /// </summary>
    internal static async Task<int?> RefuseUnlessUpToDateAsync(SchemaStatus status, InvocationConfiguration streams)
    {
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

            _ => null,
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
            Description = "Write the SQL of the move from --from to --to into --output instead of applying it. Opens no connection.",
        };

        public Option<string> Output { get; } = new("--output")
        {
            Description = "With --script: the file the SQL is written to. An existing file is replaced.",
        };

        public Option<string> From { get; } = new("--from")
        {
            Description = "With --script: the migration the script starts from, given as --to is. Default: 0, an empty schema.",
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



    /// <summary>What <c>--script</c> was asked to write.</summary>
    /// <param name="Output">The file the SQL goes to.</param>
    /// <param name="From">The text of <c>--from</c>, or <see langword="null"/> for an empty schema.</param>
    /// <param name="To">The text of <c>--to</c>, or <see langword="null"/> for the latest migration.</param>
    /// <param name="Confirmed">Whether <c>--confirm-data-loss</c> was given.</param>
    private sealed record ScriptRequest(string Output, string? From, string? To, bool Confirmed);



    /// <summary>What a move of the database was asked to do.</summary>
    /// <param name="To">The text of <c>--to</c>, or <see langword="null"/> for the latest migration.</param>
    /// <param name="Confirmed">Whether <c>--confirm-data-loss</c> was given.</param>
    /// <param name="Roles">The SQL that makes the API's database user and adds it to its roles.</param>
    private sealed record MoveRequest(string? To, bool Confirmed, RoleScript Roles);



    /// <summary>A move that passed every check and is ready to run.</summary>
    /// <param name="Plan">What to apply and what to roll back.</param>
    /// <param name="TargetId">The id of the migration the database ends on, or <c>0</c>.</param>
    /// <param name="Roles">The SQL that makes the API's database user and adds it to its roles.</param>
    private sealed record Move(MigrationPlan Plan, string TargetId, RoleScript Roles);
}
