namespace Wolfgang.Barefoot.Contracts.Dashboard;

/// <summary>
/// One room whose price moved between the caller's last two runs: a row of the Dashboard's Biggest
/// movers list (DESIGN §12).
/// </summary>
/// <param name="SearchRunId">The id of the search's part of the latest run, which the Results screen takes.</param>
/// <param name="ResortName">The resort's short name.</param>
/// <param name="RoomName">The room's name.</param>
/// <param name="CheckIn">The check-in date.</param>
/// <param name="Nights">How many nights the stay is.</param>
/// <param name="Change">The latest price less the previous one: negative for a drop.</param>
public sealed record PriceMoverResponse
(
    Guid SearchRunId,
    string ResortName,
    string RoomName,
    DateOnly CheckIn,
    int Nights,
    decimal Change
);
