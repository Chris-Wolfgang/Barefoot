using Wolfgang.Barefoot.Contracts.Runs;

namespace Wolfgang.Barefoot.Contracts.SystemStatus;

/// <summary>
/// The Admin's landing page: the last run, the budget and the health of the installation
/// (DESIGN §12, System dashboard).
/// </summary>
/// <param name="LastRun">The most recent run that is over, or <see langword="null"/> when there has been none.</param>
/// <param name="Budget">How much of the free allowances is used.</param>
/// <param name="MonthSentDigestCount">How many digests were sent this calendar month.</param>
/// <param name="MonthFailedDigestCount">How many could not be delivered this calendar month.</param>
/// <param name="WorkerName">The worker that last claimed work, or <see langword="null"/> when none ever has.</param>
/// <param name="WorkerLastSeenUtc">When it last claimed work, or <see langword="null"/>.</param>
/// <param name="UserCount">How many people have signed in at least once.</param>
/// <param name="ActiveUserCount">How many of them have an active search.</param>
/// <param name="ActiveSearchCount">How many searches are active, over everyone.</param>
/// <param name="NeedsAttention">What the Admin should look at, the most pressing first. It is short.</param>
public sealed record SystemDashboardResponse
(
    RunHistoryResponse? LastRun,
    BudgetResponse Budget,
    int MonthSentDigestCount,
    int MonthFailedDigestCount,
    string? WorkerName,
    DateTimeOffset? WorkerLastSeenUtc,
    int UserCount,
    int ActiveUserCount,
    int ActiveSearchCount,
    IReadOnlyList<AttentionItemResponse> NeedsAttention
);
