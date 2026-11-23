namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One vocabulary value a search filters rooms by: a level, a view or a type a room may have, or a
/// feature it must have. Which of those it is follows from the value's kind. A search names a value
/// at most once.
/// </summary>
public sealed class SearchRoomFilter
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="SearchMatrix"/> that filters by the value.</summary>
    public required Guid SearchMatrixId { get; set; }



    /// <summary>The <see cref="VocabularyValue"/> it filters by.</summary>
    public required Guid VocabularyValueId { get; set; }
}
