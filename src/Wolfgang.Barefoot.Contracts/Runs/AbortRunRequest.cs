namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// The body of <c>POST /runs/{id}/abort</c>: the worker stopped the run, and the rest waits for a
/// resume (DESIGN §5).
/// </summary>
/// <param name="Reason">Why the worker stopped. Shown to an Admin only (ADR 0006).</param>
public sealed record AbortRunRequest(string Reason);
