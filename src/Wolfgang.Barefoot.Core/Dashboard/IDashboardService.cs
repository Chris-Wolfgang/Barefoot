using Wolfgang.Barefoot.Contracts.Dashboard;

namespace Wolfgang.Barefoot.Core.Dashboard;

/// <summary>
/// Reads a person's Dashboard (DESIGN §12): what changed since their last digest. Everything here is
/// about the caller's own searches, for an Admin too; the Admin's view of the whole system is the
/// System dashboard, which is read elsewhere.
/// </summary>
/// <remarks>
/// Nothing here changes anything. The Dashboard's Open issues card is read from the issue service.
/// </remarks>
public interface IDashboardService
{
    /// <summary>
    /// The status strip, the biggest movers and the latest digest.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<DashboardResponse> GetAsync(CancellationToken cancellationToken);



    /// <summary>
    /// The caller's searches on their last ten runs, one row for each search on each run, the latest
    /// run first. The list is short and is not paged. The screen reads it again every 30 seconds
    /// while a run is in progress.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<IReadOnlyList<RecentResultResponse>> ListRecentResultsAsync(CancellationToken cancellationToken);
}
