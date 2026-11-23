namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// The run in flight, as the Admin's Runs page shows it while it happens (DESIGN §12).
/// </summary>
/// <param name="Id">The run's id.</param>
/// <param name="Trigger">What started the run, as the caller named it.</param>
/// <param name="StartedUtc">When the run was created.</param>
/// <param name="WorkerName">
/// The worker that last claimed work, or <see langword="null"/> when nothing has been claimed yet.
/// </param>
/// <param name="LastClaimUtc">
/// When work was last claimed, or <see langword="null"/> when nothing has been. A long silence means
/// the worker has stopped.
/// </param>
/// <param name="Lookups">The run's room lookups.</param>
/// <param name="FlightSets">The run's flight sets, which start when the room lookups are done.</param>
/// <param name="Searches">The searches the run covers, in the order their work is handed out.</param>
public sealed record RunProgressResponse
(
    Guid Id,
    string Trigger,
    DateTimeOffset StartedUtc,
    string? WorkerName,
    DateTimeOffset? LastClaimUtc,
    WorkProgress Lookups,
    WorkProgress FlightSets,
    IReadOnlyList<SearchProgressResponse> Searches
);
