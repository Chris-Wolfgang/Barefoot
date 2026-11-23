using Wolfgang.Extensions.IComparable;

namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// A stretch of days during which a resort had one status (DESIGN §7). The spans of a resort are
/// kept by the catalog snapshot that notices a resort appear or disappear; nobody edits them.
/// </summary>
public sealed class ResortSpan
{
    /// <summary>The first day of a span whose start is not known.</summary>
    public static readonly DateOnly OpenStart = new(1900, 1, 1);

    /// <summary>The last day of a span that has not ended.</summary>
    public static readonly DateOnly OpenEnd = new(9999, 12, 31);



    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="Resort"/> the span is about.</summary>
    public required Guid ResortId { get; set; }



    /// <summary>The first day of the span, or <see cref="OpenStart"/>.</summary>
    public DateOnly EffDate { get; set; } = OpenStart;



    /// <summary>The last day of the span, or <see cref="OpenEnd"/>.</summary>
    public DateOnly EndDate { get; set; } = OpenEnd;



    /// <summary>The resort's status on every day of the span.</summary>
    public required ResortSpanStatus Status { get; set; }



    /// <summary>
    /// Whether <paramref name="date"/> falls in the span. Both the first and the last day count.
    /// </summary>
    /// <param name="date">The day asked about.</param>
    public bool Covers(DateOnly date)
    {
        return date.IsInRange(EffDate, EndDate);
    }
}
