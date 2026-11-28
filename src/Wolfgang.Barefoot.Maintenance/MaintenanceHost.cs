using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;
using Wolfgang.Barefoot.Hosting;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// Runs one <c>bf-maint</c> command line and returns its exit code (docs/MIGRATE.md). It writes a
/// verb's own output to <c>output</c> and everything else to <c>error</c>, so a test can capture both.
/// The log gets the command line, every line written to <c>error</c>, and the exit code.
/// </summary>
internal static class MaintenanceHost
{
    /// <summary>
    /// Parses <paramref name="args"/>, runs the verb they name and returns its <see cref="ExitCode"/>
    /// as a number. Arguments that do not parse exit <see cref="ExitCode.Usage"/>; an exception no verb
    /// handled is logged as an error, which is one line on stderr and the stack trace in the file, and
    /// exits <see cref="ExitCode.Failed"/>.
    /// </summary>
    /// <param name="args">The command line, without the name of the executable.</param>
    /// <param name="output">Where a verb writes its own output.</param>
    /// <param name="error">Where everything else is written.</param>
    /// <param name="log">The log of this run. Its warnings and errors reach the same stderr as <paramref name="error"/>.</param>
    /// <param name="configuration">Holds the connection string <see cref="MigrateCommand.ConnectionStringName"/>.</param>
    /// <param name="openContext">Makes a context on the database a connection string names.</param>
    /// <param name="offlineContext">Makes a context that names no database.</param>
    /// <param name="roles">The SQL that makes the API's database user and adds it to its roles.</param>
    /// <param name="scriptPreamble">What a script from <c>migrate --script</c> starts with.</param>
    /// <param name="catalogQuery">The query <c>verify-schema</c> reads the database's schema with.</param>
    /// <param name="cancellationToken">Cancels the verb.</param>
    public static async Task<int> RunAsync
    (
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        ILogger log,
        IConfiguration configuration,
        Func<string, BarefootDbContext> openContext,
        Func<BarefootDbContext> offlineContext,
        RoleScript roles,
        string scriptPreamble,
        string catalogQuery,
        CancellationToken cancellationToken = default
    )
    {
        log.LogInformation("bf-maint {Arguments}", string.Join(' ', args));

        using var errors = new LoggedWriter(error, log);

        var exitCode = await RunVerbAsync
            (
                args,
                new InvocationConfiguration
                {
                    Output = output,
                    Error = errors,
                    EnableDefaultExceptionHandler = false,
                },
                log,
                new RootCommand("Changes and reports the schema of Barefoot's database.")
                {
                    MigrateCommand.Create(configuration, openContext, offlineContext, roles, scriptPreamble),
                    GrantRolesCommand.Create(configuration, openContext, roles),
                    AssignOwnerCommand.Create(configuration, openContext),
                    VerifySchemaCommand.Create(configuration, openContext, catalogQuery),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        log.LogInformation("exit code {Code} ({Name})", exitCode, ((ExitCode)exitCode).ToString());

        return exitCode;
    }



    private static async Task<int> RunVerbAsync
    (
        IReadOnlyList<string> args,
        InvocationConfiguration invocation,
        ILogger log,
        RootCommand root,
        CancellationToken cancellationToken
    )
    {
        var parsed = root.Parse(args);
        if (parsed.Errors.Count > 0)
        {
            foreach (var parseError in parsed.Errors)
            {
                await invocation
                    .Error
                    .WriteLineAsync(parseError.Message)
                    .ConfigureAwait(false);
            }

            return (int)ExitCode.Usage;
        }

        try
        {
            return await parsed
                .InvokeAsync(invocation, cancellationToken)
                .ConfigureAwait(false);
        }
#pragma warning disable CA1031 // The host is the last place an exception can be turned into an exit code (docs/MIGRATE.md, Output and logging).
        catch (Exception exception)
#pragma warning restore CA1031
        {
            log.LogError(exception, "bf-maint failed: {Reason}", exception.Message);

            return (int)ExitCode.Failed;
        }
    }
}
