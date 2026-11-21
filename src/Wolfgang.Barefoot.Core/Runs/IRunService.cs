using Wolfgang.Barefoot.Contracts.Runs;
using Wolfgang.Barefoot.Contracts.Scraping;

namespace Wolfgang.Barefoot.Core.Runs;

/// <summary>
/// The run lifecycle as a worker drives it (ADR 0003; DESIGN §5): start or resume a run, claim one
/// lookup at a time and report its result, then the flight sets the same way, complete each search
/// and finish.
/// </summary>
/// <remarks>
/// The API owns every decision about data: which lookups exist, who holds a lease and for how long,
/// and what a run's status is. Reporting a result and completing a search are safe to repeat;
/// starting, claiming, aborting and finishing are not (ADR 0012 §4).
/// </remarks>
public interface IRunService
{
    /// <summary>
    /// Creates a run and its whole work list. Refused while another run is in progress.
    /// </summary>
    /// <param name="request">What started the run.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<RunResponse> StartAsync(StartRunRequest request, CancellationToken cancellationToken);



    /// <summary>
    /// The run that was left incomplete, ready to be continued, or <see langword="null"/> when there is none.
    /// </summary>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<RunResponse?> ResumeAsync(CancellationToken cancellationToken);



    /// <summary>
    /// Leases the next room lookup of the run, in run order, or returns <see langword="null"/> when none is left.
    /// </summary>
    /// <param name="runId">The run.</param>
    /// <param name="request">Who claims.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<ClaimedLookup?> ClaimLookupAsync
    (
        Guid runId,
        ClaimRequest request,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Reports what a room lookup found, or that it failed.
    /// </summary>
    /// <param name="runId">The run.</param>
    /// <param name="requestId">The request that was claimed.</param>
    /// <param name="result">The rooms or the error, with the attempt token of the claim.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task ReportRoomsAsync
    (
        Guid runId,
        Guid requestId,
        RoomsResult result,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Leases the next flight set of the run, or returns <see langword="null"/> when none is left.
    /// </summary>
    /// <param name="runId">The run.</param>
    /// <param name="request">Who claims.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<ClaimedFlightSet?> ClaimFlightSetAsync
    (
        Guid runId,
        ClaimRequest request,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Reports the round trips a flight-set lookup found, or that it failed.
    /// </summary>
    /// <param name="runId">The run.</param>
    /// <param name="flightSetId">The flight set that was claimed.</param>
    /// <param name="result">The round trips or the error, with the attempt token of the claim.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task ReportFlightsAsync
    (
        Guid runId,
        Guid flightSetId,
        FlightsResult result,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Aggregates one search's results for the run and stores its digest. Nothing is sent.
    /// </summary>
    /// <param name="runId">The run.</param>
    /// <param name="searchId">The search whose work is done.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<SearchRunResponse> CompleteSearchAsync
    (
        Guid runId,
        Guid searchId,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Records that the worker stopped the run. What is left waits for a resume.
    /// </summary>
    /// <param name="runId">The run.</param>
    /// <param name="request">Why the worker stopped.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task AbortAsync
    (
        Guid runId,
        AbortRunRequest request,
        CancellationToken cancellationToken
    );



    /// <summary>
    /// Ends the run. The API derives its status from what was and was not done.
    /// </summary>
    /// <param name="runId">The run.</param>
    /// <param name="cancellationToken">Stops the call.</param>
    Task<RunResponse> FinishAsync(Guid runId, CancellationToken cancellationToken);
}
