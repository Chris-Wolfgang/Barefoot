namespace Wolfgang.Barefoot.Contracts.Issues;

/// <summary>
/// Something that needs a person's attention, as the Dashboard card and the pop-up after sign-in show
/// it (ADR 0008 §7). An issue is listed until what it is about is put right; acknowledging it only
/// stops the pop-up.
/// </summary>
/// <param name="Id">The issue's id.</param>
/// <param name="Kind">What the issue is about.</param>
/// <param name="Subject">The issue in one sentence, naming the search, resort or airport.</param>
/// <param name="ResolvesWhen">What will make the issue go away, in one sentence.</param>
/// <param name="TargetId">
/// The id of what the screen's action opens: the search for a passed window or an offline resort,
/// the digest for a failed delivery, the airport for a missing time zone. It is
/// <see langword="null"/> when the issue is about no single thing.
/// </param>
/// <param name="RaisedUtc">When the issue was raised.</param>
/// <param name="AcknowledgedUtc">
/// When the person acknowledged it, or <see langword="null"/> while it is still new to them and so
/// in the pop-up.
/// </param>
public sealed record OpenIssueResponse
(
    Guid Id,
    IssueKind Kind,
    string Subject,
    string ResolvesWhen,
    Guid? TargetId,
    DateTimeOffset RaisedUtc,
    DateTimeOffset? AcknowledgedUtc
);
