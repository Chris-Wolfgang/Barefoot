using System.Globalization;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;
using Wolfgang.Barefoot.Hosting;
using Wolfgang.Barefoot.TestKit;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// One run of <c>bf-maint</c> in this process: its exit code, what it wrote to each stream, and what
/// it wrote to its log file.
/// </summary>
internal sealed record HostRun(ExitCode ExitCode, string Output, string Error, string Log)
{
    /// <summary>
    /// A role script for a server that cannot make the user: it asks, is told no, and runs nothing.
    /// SQLite can run it, which it cannot do with the script for SQL Server.
    /// </summary>
    public static readonly RoleScript NoRoles = new("SELECT 0 AS \"Value\"", [], "not this kind of server");



    /// <summary>
    /// Runs <see cref="MaintenanceHost"/> with <paramref name="args"/> and captures both streams. The
    /// log file goes in a folder of its own under the temporary directory, which is removed afterwards.
    /// </summary>
    public static async Task<HostRun> StartAsync
    (
        string[] args,
        IConfiguration configuration,
        Func<string, BarefootDbContext> openContext,
        CancellationToken cancellationToken = default,
        Func<BarefootDbContext>? offlineContext = null,
        RoleScript? roles = null,
        string scriptPreamble = ""
    )
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        var folder = Path.Combine(Path.GetTempPath(), $"barefoot-{Guid.NewGuid():N}");
        try
        {
            int exitCode;
            string logFile;
            using (var log = HostLog.Start(new HostLogOptions(Program.HostName, folder, error, TestClock.Create())))
            {
                logFile = log.FilePath!;
                exitCode = await MaintenanceHost.RunAsync
                (
                    args,
                    output,
                    error,
                    log
                        .Factory
                        .CreateLogger(Program.HostName),
                    configuration,
                    openContext,
                    offlineContext ?? SqlServerDatabase.Offline,
                    roles ?? NoRoles,
                    scriptPreamble,
                    cancellationToken
                );
            }

            return new HostRun
            (
                (ExitCode)exitCode,
                output.ToString(),
                error.ToString(),
                await File.ReadAllTextAsync(logFile, CancellationToken.None)
            );
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
