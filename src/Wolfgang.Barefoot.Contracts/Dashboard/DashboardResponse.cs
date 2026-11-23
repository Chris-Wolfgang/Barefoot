namespace Wolfgang.Barefoot.Contracts.Dashboard;

/// <summary>
/// What a person's Dashboard shows besides its two lists: the status strip, the biggest movers and
/// the latest digest (DESIGN §12). Recent results and Open issues are read on their own, because
/// they refresh on their own.
/// </summary>
/// <param name="ActiveSearchCount">How many of the caller's searches are active. Zero is the first-sign-in Dashboard.</param>
/// <param name="LastSearchedUtc">
/// When a run last searched for the caller, or <see langword="null"/> when none has.
/// </param>
/// <param name="NextRunUtc">
/// When the next run is expected, or <see langword="null"/> when the API does not know. Runs are
/// started by a scheduler outside the API.
/// </param>
/// <param name="LastDigestUtc">
/// When a digest was last sent to the caller, or <see langword="null"/> when none has been.
/// </param>
/// <param name="MoversSinceUtc">
/// When the run the movers are compared with started, or <see langword="null"/> when the caller has
/// fewer than two runs to compare.
/// </param>
/// <param name="Movers">The rooms whose price moved most since then, the biggest move first. It is short.</param>
/// <param name="LatestDigest">The digest most recently sent to the caller, or <see langword="null"/>.</param>
public sealed record DashboardResponse
(
    int ActiveSearchCount,
    DateTimeOffset? LastSearchedUtc,
    DateTimeOffset? NextRunUtc,
    DateTimeOffset? LastDigestUtc,
    DateTimeOffset? MoversSinceUtc,
    IReadOnlyList<PriceMoverResponse> Movers,
    LatestDigestResponse? LatestDigest
);
