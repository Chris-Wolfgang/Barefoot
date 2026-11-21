namespace Wolfgang.Barefoot.Contracts.Digests;

/// <summary>
/// Narrows a digest list (ADR 0006 §5). Every member is optional; with none set the list is not narrowed.
/// </summary>
/// <param name="SearchId">Only digests of this search.</param>
/// <param name="From">Only digests stored on or after this date.</param>
/// <param name="To">Only digests stored on or before this date.</param>
/// <param name="Text">Only digests that contain this text.</param>
/// <param name="UndeliveredOnly">Only digests that were not sent.</param>
public sealed record DigestFilter
(
    Guid? SearchId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Text = null,
    bool UndeliveredOnly = false
);
