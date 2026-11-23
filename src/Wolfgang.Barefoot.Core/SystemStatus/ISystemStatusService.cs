using Wolfgang.Barefoot.Contracts.SystemStatus;

namespace Wolfgang.Barefoot.Core.SystemStatus;

/// <summary>
/// Reads the state of the installation for the Admin (DESIGN §12): the System dashboard and which
/// builds are running. Only an Admin may call it.
/// </summary>
/// <remarks>
/// Nothing here changes anything. Whether the API can serve at all is not here: that is its
/// readiness, which every client reads before signing in (ADR 0001).
/// </remarks>
public interface ISystemStatusService
{
    /// <summary>
    /// The last run, the budget and what needs attention.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<SystemDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);



    /// <summary>
    /// The API build, the database schema and the worker last seen.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<RunningResponse> GetRunningAsync(CancellationToken cancellationToken);
}
