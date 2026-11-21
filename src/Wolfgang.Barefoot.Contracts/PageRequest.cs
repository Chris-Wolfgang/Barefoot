namespace Wolfgang.Barefoot.Contracts;

/// <summary>
/// Which page of a list to return (ADR 0012 §5). Every member is optional: with none set, the first
/// page comes back in the list's default order and size.
/// </summary>
/// <param name="After">The <see cref="Page{T}.NextCursor"/> of the page before the one wanted.</param>
/// <param name="Before">The <see cref="Page{T}.PreviousCursor"/> of the page after the one wanted.</param>
/// <param name="Sort">
/// One field name; a leading <c>-</c> means descending. A cursor is bound to the sort it was issued under.
/// </param>
/// <param name="Size">The number of rows wanted. The API's default is 25 and its cap 200.</param>
public sealed record PageRequest
(
    string? After = null,
    string? Before = null,
    string? Sort = null,
    int? Size = null
);
