namespace Wolfgang.Barefoot.Contracts;

/// <summary>
/// One page of a list (ADR 0012 §5). Paging is by keyset: a page names its neighbours with opaque
/// cursors, and there are no page numbers.
/// </summary>
/// <typeparam name="T">The record each row of the list is returned as.</typeparam>
/// <param name="Items">The rows of this page, in the order of the requested sort.</param>
/// <param name="NextCursor">
/// The cursor to send as <c>after</c> for the next page, or <see langword="null"/> on the last page.
/// </param>
/// <param name="PreviousCursor">
/// The cursor to send as <c>before</c> for the previous page, or <see langword="null"/> on the first page.
/// </param>
/// <param name="FirstItemIndex">The position in the whole list of this page's first row.</param>
/// <param name="TotalCount">
/// The number of rows the list had when it was first requested. It is counted once and carried in
/// the cursor, so it can be slightly stale on a later page.
/// </param>
public sealed record Page<T>
(
    IReadOnlyList<T> Items,
    string? NextCursor,
    string? PreviousCursor,
    int FirstItemIndex,
    int TotalCount
);
