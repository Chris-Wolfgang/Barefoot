namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// The values the API hands a worker with its run, so that the scraper reads no settings of its own
/// (ADR 0011 §9). They are read when the call is answered, so a resumed run picks up a change.
/// </summary>
/// <param name="PolitenessDelayMs">The pause between two lookups, in milliseconds.</param>
/// <param name="PolitenessJitterMs">The most that is added to the pause at random, in milliseconds.</param>
public sealed record WorkerSettings(int PolitenessDelayMs, int PolitenessJitterMs);
