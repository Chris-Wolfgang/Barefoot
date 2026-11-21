namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// A run, as <c>POST /runs</c>, <c>POST /runs/resume</c> and <c>POST /runs/{id}/finish</c> return it
/// (DESIGN §5).
/// </summary>
/// <param name="Id">The run's id.</param>
/// <param name="Status">The run's state.</param>
/// <param name="SearchIds">The searches the run covers; the worker completes each one when its work is done.</param>
/// <param name="Worker">The values the worker must use for this run.</param>
public sealed record RunResponse
(
    Guid Id,
    RunStatus Status,
    IReadOnlyList<Guid> SearchIds,
    WorkerSettings Worker
);
