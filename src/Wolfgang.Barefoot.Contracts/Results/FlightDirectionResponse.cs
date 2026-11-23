namespace Wolfgang.Barefoot.Contracts.Results;

/// <summary>
/// One direction of a round trip: the way out or the way back.
/// </summary>
/// <param name="Legs">The flights in the order they are flown.</param>
/// <param name="StopCount">The number of stops: one less than the number of flights.</param>
/// <param name="DurationMinutes">
/// The time from the first departure to the last arrival, layovers included. It is
/// <see langword="null"/> when an airport on the way has no time zone, because the times then cannot
/// be compared (DESIGN §7).
/// </param>
/// <param name="IsOvernight">Whether the site marks the direction as overnight.</param>
public sealed record FlightDirectionResponse
(
    IReadOnlyList<FlightLegResponse> Legs,
    int StopCount,
    int? DurationMinutes,
    bool IsOvernight
);
