using Microsoft.Extensions.Configuration;

namespace Wolfgang.Barefoot.Maintenance;

/// <summary>
/// The entry point of <c>bf-maint</c>. It reads the configuration, hands the host the console's two
/// streams and SQL Server, and returns the host's exit code.
/// </summary>
internal static class Program
{
    internal static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

#pragma warning disable RS0030 // Only Program names Console: it passes the two streams to the host, which writes to what it is handed.
        return MaintenanceHost.RunAsync(args, Console.Out, Console.Error, configuration, SqlServerDatabase.Open);
#pragma warning restore RS0030
    }
}
