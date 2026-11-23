namespace Wolfgang.Barefoot.Core.Digests;

/// <summary>
/// One resort's part of a digest: a line of the summary and a section of room cards (DESIGN §9).
/// </summary>
/// <param name="Name">The resort's name as the summary shows it.</param>
/// <param name="FromPrice">The cheapest total at the resort, or <see langword="null"/> when nothing matched.</param>
/// <param name="PriceChange">
/// How the cheapest total moved since the run before: negative is cheaper, zero unchanged,
/// <see langword="null"/> when there is nothing to compare with.
/// </param>
/// <param name="Offline">Whether the resort was offline on the booking site and skipped.</param>
/// <param name="Rooms">The room cards, cheapest first.</param>
/// <param name="MoreCount">How many further options matched and are not shown.</param>
/// <param name="ResultsUrl">The link that opens this resort's results.</param>
public sealed record DigestResort
(
    string Name,
    decimal? FromPrice,
    decimal? PriceChange,
    bool Offline,
    IReadOnlyList<DigestRoom> Rooms,
    int MoreCount,
    Uri ResultsUrl
);
