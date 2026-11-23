namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// What <c>POST /runs/{id}/searches/{s}/complete</c> returns: how the run went for one search. A
/// repeat of the call returns the same answer and stores no second digest (ADR 0012 §4).
/// </summary>
/// <param name="SearchId">The search.</param>
/// <param name="Outcome">The run's outcome for that search.</param>
public sealed record SearchRunResponse(Guid SearchId, RunStatus Outcome);
