namespace Wolfgang.Barefoot.Contracts.PriceHistory;

/// <summary>
/// One run's price for a room and a check-in date: one point of the chart and one row of its table.
/// </summary>
/// <remarks>
/// A run whose lookup failed is still listed, with no prices: the chart shows a gap there, never a
/// guess.
/// </remarks>
/// <param name="CapturedUtc">When the price was read.</param>
/// <param name="RoomPrice">
/// The price of the room for the whole stay, or <see langword="null"/> when the lookup failed or the
/// room was not offered.
/// </param>
/// <param name="CheapestFare">
/// The cheapest airfare from the departure airport asked for, or <see langword="null"/> when none
/// was asked for or none was found.
/// </param>
/// <param name="Total">
/// The room price plus the cheapest airfare, or <see langword="null"/> when either is missing.
/// </param>
/// <param name="ChangeSincePrevious">
/// The change against the capture before this one that has a price: of the total when an airport was
/// asked for, of the room price otherwise. Negative is cheaper. It is <see langword="null"/> for the
/// first capture and for one without a price.
/// </param>
public sealed record PriceCaptureResponse
(
    DateTimeOffset CapturedUtc,
    decimal? RoomPrice,
    decimal? CheapestFare,
    decimal? Total,
    decimal? ChangeSincePrevious
);
