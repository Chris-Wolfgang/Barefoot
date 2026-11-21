namespace Wolfgang.Barefoot.Core.Runs;

/// <summary>
/// The complete work list of a run, known when the run is created so that progress can be counted
/// (DESIGN §5).
/// </summary>
/// <param name="Requests">Every distinct room lookup, in the order it was first wanted.</param>
/// <param name="FlightSets">Every distinct flight lookup, in the order it was first wanted.</param>
/// <param name="Items">One entry per search and room lookup it wants.</param>
public sealed record Expansion
(
    IReadOnlyList<PlannedRequest> Requests,
    IReadOnlyList<FlightSetKey> FlightSets,
    IReadOnlyList<PlannedItem> Items
);
