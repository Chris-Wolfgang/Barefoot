namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One recipient a search mails its digest to, besides its owner (ADR 0006). A search names a
/// recipient at most once. Deleting the search or the recipient removes the row.
/// </summary>
public sealed class SearchRecipient
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="SearchMatrix"/> that mails the recipient.</summary>
    public required Guid SearchMatrixId { get; set; }



    /// <summary>The <see cref="Recipient"/> it mails.</summary>
    public required Guid RecipientId { get; set; }
}
