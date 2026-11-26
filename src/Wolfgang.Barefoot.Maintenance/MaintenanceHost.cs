using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// Runs one <c>bf-maint</c> command line and returns its exit code (docs/MIGRATE.md). It writes a
/// verb's own output to <c>output</c> and everything else to <c>error</c>, so a test can capture both.
/// </summary>
internal static class MaintenanceHost
{
    /// <summary>
    /// Parses <paramref name="args"/>, runs the verb they name and returns its <see cref="ExitCode"/>
    /// as a number. Arguments that do not parse exit <see cref="ExitCode.Usage"/>; an exception no verb
    /// handled is one line on <paramref name="error"/> and exits <see cref="ExitCode.Failed"/>.
    /// </summary>
    /// <param name="args">The command line, without the name of the executable.</param>
    /// <param name="output">Where a verb writes its own output.</param>
    /// <param name="error">Where everything else is written.</param>
    /// <param name="configuration">Holds the connection string <see cref="MigrateCommand.ConnectionStringName"/>.</param>
    /// <param name="openContext">Makes a context on the database a connection string names.</param>
    /// <param name="cancellationToken">Cancels the verb.</param>
    public static async Task<int> RunAsync
    (
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        IConfiguration configuration,
        Func<string, BarefootDbContext> openContext,
        CancellationToken cancellationToken = default
    )
    {
        var root = new RootCommand("Changes and reports the schema of Barefoot's database.")
        {
            MigrateCommand.Create(configuration, openContext),
        };

        var parsed = root.Parse(args);
        if (parsed.Errors.Count > 0)
        {
            foreach (var parseError in parsed.Errors)
            {
                await error
                    .WriteLineAsync(parseError.Message)
                    .ConfigureAwait(false);
            }

            return (int)ExitCode.Usage;
        }

        var invocation = new InvocationConfiguration
        {
            Output = output,
            Error = error,
            EnableDefaultExceptionHandler = false,
        };

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
            await error
                .WriteLineAsync($"bf-maint failed: {exception.Message}")
                .ConfigureAwait(false);

            return (int)ExitCode.Failed;
        }
    }
}
