using Wolfgang.Barefoot.Contracts;
using Wolfgang.Barefoot.Contracts.Runs;

namespace Wolfgang.Barefoot.Core.Runs;

/// <summary>
/// Reads runs for the Admin's Runs page (DESIGN §12): the one in flight and the history. Only an
/// Admin may call it.
/// </summary>
/// <remarks>
/// Nothing here changes anything, and nothing here starts a run: runs are started by the worker
/// through <see cref="IRunService"/>.
/// </remarks>
public interface IRunMonitorService
{
    /// <summary>
    /// The run in progress, or <see langword="null"/> when there is none. The page reads it again
    /// every 30 seconds for as long as the last answer was a run.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<RunProgressResponse?> GetCurrentAsync(CancellationToken cancellationToken);



    /// <summary>
    /// One page of the runs that are over, the latest first.
    /// </summary>
    /// <param name="page">Which page.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<Page<RunHistoryResponse>> ListHistoryAsync(PageRequest page, CancellationToken cancellationToken);
}
