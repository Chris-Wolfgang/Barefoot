using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Core.Runs;

/// <summary>
/// The link between one search and one room lookup it wants, with the flight lookups that go with it:
/// one per departure airport (ADR 0004 §2 and §6).
/// </summary>
/// <param name="SearchId">The search.</param>
/// <param name="Request">The room lookup.</param>
/// <param name="Cabin">The search's cabin, or <see langword="null"/> when it does not include flights.</param>
/// <param name="FlightSets">The flight lookups, one per departure airport; empty without flights.</param>
public sealed record PlannedItem
(
    Guid SearchId,
    LookupKey Request,
    Cabin? Cabin,
    IReadOnlyList<FlightSetKey> FlightSets
);
