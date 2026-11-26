using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Hosting;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// The entry point of <c>bf-maint</c>. It reads the configuration, starts the log, hands the host the
/// console's two streams and SQL Server, and returns the host's exit code.
/// </summary>
internal static class Program
{
    /// <summary>The name of the executable, which names the log file and the logger.</summary>
    internal const string HostName = "bf-maint";



    internal static Task<int> Main(string[] args)
    {
        return RunAsync(args, LogFolder.Default());
    }



    /// <summary>
    /// Runs one command line with the log file in <paramref name="logFolder"/>.
    /// </summary>
    /// <param name="args">The command line, without the name of the executable.</param>
    /// <param name="logFolder">The folder the log file goes in.</param>
    internal static async Task<int> RunAsync(string[] args, string logFolder)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

#pragma warning disable RS0030 // Only Program names Console and the system clock: it passes them to the log and the host, which use what they are handed.
        using var log = HostLog.Start
        (
            new HostLogOptions(HostName, logFolder, Console.Error, TimeProvider.System)
        );

        return await MaintenanceHost
            .RunAsync
            (
                args,
                Console.Out,
                Console.Error,
                log
                    .Factory
                    .CreateLogger(HostName),
                configuration,
                SqlServerDatabase.Open,
                SqlServerDatabase.Offline,
                SqlServerDatabase.Roles
            )
            .ConfigureAwait(false);
#pragma warning restore RS0030
    }
}
