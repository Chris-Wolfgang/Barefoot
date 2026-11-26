namespace Wolfgang.Barefoot.Hosting;

/// <summary>
/// What <see cref="HostLog.Start"/> needs to know about the host it logs for.
/// </summary>
/// <param name="HostName">The name of the executable, which starts the name of every log file (<c>bf-maint</c>).</param>
/// <param name="Folder">The folder the log file goes in (<see cref="LogFolder.Default"/>). An empty text means there is none.</param>
/// <param name="Error">The host's stderr. It takes warnings and errors.</param>
/// <param name="Clock">Gives the day, in UTC, that names the log file.</param>
public sealed record HostLogOptions
(
    string HostName,
    string Folder,
    TextWriter Error,
    TimeProvider Clock
);
