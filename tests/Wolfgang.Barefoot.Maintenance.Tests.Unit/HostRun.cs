using System.Globalization;
using Microsoft.Extensions.Configuration;
using Wolfgang.Barefoot.Core.Hosting;
using Wolfgang.Barefoot.Data;

namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// One run of <c>bf-maint</c> in this process: its exit code and what it wrote to each stream.
/// </summary>
internal sealed record HostRun(ExitCode ExitCode, string Output, string Error)
{
    /// <summary>
    /// A role script for a server that cannot make the user: it asks, is told no, and runs nothing.
    /// SQLite can run it, which it cannot do with the script for SQL Server.
    /// </summary>
    public static readonly RoleScript NoRoles = new("SELECT 0 AS \"Value\"", [], "not this kind of server");



    /// <summary>
    /// Runs <see cref="MaintenanceHost"/> with <paramref name="args"/> and captures both streams.
    /// </summary>
    public static async Task<HostRun> StartAsync
    (
        string[] args,
        IConfiguration configuration,
        Func<string, BarefootDbContext> openContext,
        CancellationToken cancellationToken = default,
        Func<BarefootDbContext>? offlineContext = null,
        RoleScript? roles = null
    )
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var error = new StringWriter(CultureInfo.InvariantCulture);

        var exitCode = await MaintenanceHost.RunAsync
        (
            args,
            output,
            error,
            configuration,
            openContext,
            offlineContext ?? SqlServerDatabase.Offline,
            roles ?? NoRoles,
            cancellationToken
        );

        return new HostRun((ExitCode)exitCode, output.ToString(), error.ToString());
    }
}
