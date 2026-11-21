namespace Wolfgang.Barefoot.Core.Runs;

/// <summary>
/// One room lookup a run will perform, shared by every search that wants it.
/// </summary>
/// <param name="Key">What is looked up.</param>
/// <param name="RunOrder">
/// When it is handed out relative to the others: the highest run order among the searches that want
/// it (ADR 0004 §1).
/// </param>
public sealed record PlannedRequest(LookupKey Key, int RunOrder);
