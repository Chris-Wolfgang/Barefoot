using Wolfgang.Barefoot.Contracts.Runs;

namespace Wolfgang.Barefoot.Contracts.Results;

/// <summary>
/// One entry of the Results screen's run picker: one run of one search. A person who is not an Admin
/// never sees a run as an object (DESIGN §12), so the id is the search's own part of the run.
/// </summary>
/// <param name="SearchRunId">The id of the search's part of the run. Results are read by it.</param>
/// <param name="CapturedUtc">The instant the picker shows for the run.</param>
/// <param name="Outcome">How the run went for this search.</param>
public sealed record ResultRunResponse
(
    Guid SearchRunId,
    DateTimeOffset CapturedUtc,
    RunStatus Outcome
);
