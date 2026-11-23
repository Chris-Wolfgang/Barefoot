namespace Wolfgang.Barefoot.Contracts.PriceHistory;

/// <summary>
/// How one room on one check-in date has been priced, run after run (DESIGN §12, Price history).
/// The chart and its table are drawn from the same captures.
/// </summary>
/// <param name="NightCount">The length of the stay the prices are for.</param>
/// <param name="AdultCount">The number of adults the prices are for.</param>
/// <param name="ChildCount">The number of children the prices are for.</param>
/// <param name="InfantCount">The number of infants the prices are for.</param>
/// <param name="Captures">Every capture in the range asked for, oldest first. The list is not paged.</param>
public sealed record PriceHistoryResponse
(
    int NightCount,
    int AdultCount,
    int ChildCount,
    int InfantCount,
    IReadOnlyList<PriceCaptureResponse> Captures
);
