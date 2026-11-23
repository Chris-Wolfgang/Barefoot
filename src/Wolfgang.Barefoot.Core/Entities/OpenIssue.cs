using Wolfgang.Barefoot.Contracts.Issues;

namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// Something that needs one person's attention until it is put right (ADR 0008 §7). Acknowledging
/// an issue stops the pop-up; only what resolves it removes it from the list.
/// </summary>
public sealed class OpenIssue
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="AppUser"/> the issue is shown to.</summary>
    public required Guid UserId { get; set; }



    /// <summary>What the issue is about.</summary>
    public required IssueKind Kind { get; set; }



    /// <summary>The issue in one sentence, naming the search, resort or airport.</summary>
    public required string Subject { get; set; }



    /// <summary>What will make the issue go away, in one sentence.</summary>
    public required string ResolvesWhen { get; set; }



    /// <summary>
    /// The id of what the screen's action opens (a search, a digest or an airport), or
    /// <see langword="null"/> when the issue is about no single thing.
    /// </summary>
    public Guid? TargetId { get; set; }



    /// <summary>When the issue was raised.</summary>
    public DateTimeOffset RaisedUtc { get; set; }



    /// <summary>When the person acknowledged it, or <see langword="null"/> while it is new to them.</summary>
    public DateTimeOffset? AcknowledgedUtc { get; set; }



    /// <summary>When it was put right, or <see langword="null"/> while it is still open.</summary>
    public DateTimeOffset? ResolvedUtc { get; set; }
}
