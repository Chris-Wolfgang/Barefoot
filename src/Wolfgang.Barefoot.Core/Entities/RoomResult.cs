namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One room the site offered for one lookup (DESIGN §8). It is stored once per
/// <see cref="SearchRequest"/> and shared by every search that wanted the lookup.
/// </summary>
public sealed class RoomResult
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="SearchRequest"/> that found the room.</summary>
    public required Guid SearchRequestId { get; set; }



    /// <summary>The <see cref="RoomCategory"/> offered.</summary>
    public required Guid RoomCategoryId { get; set; }



    /// <summary>The room's level, a <see cref="VocabularyValue"/>, or <see langword="null"/> when it was not read.</summary>
    public Guid? RoomLevelId { get; set; }



    /// <summary>The room's view, a <see cref="VocabularyValue"/>, or <see langword="null"/> when it was not read.</summary>
    public Guid? RoomViewId { get; set; }



    /// <summary>The room's type, a <see cref="VocabularyValue"/>, or <see langword="null"/> when it was not read.</summary>
    public Guid? RoomTypeId { get; set; }



    /// <summary>Whether the site showed the room as sold out.</summary>
    public bool IsSoldOut { get; set; }



    /// <summary>How many rooms the site said were left, or <see langword="null"/> when it did not say.</summary>
    public int? RemainingRoomCount { get; set; }



    /// <summary>
    /// The price of the whole stay for all guests, taxes included, in US dollars, or
    /// <see langword="null"/> when the room is sold out.
    /// </summary>
    public decimal? TotalAmount { get; set; }



    /// <summary>The price before a promotion, or <see langword="null"/> when the site showed none.</summary>
    public decimal? RegularAmount { get; set; }



    /// <summary>The promotion the site showed with the price, or <see langword="null"/>.</summary>
    public string? Promotion { get; set; }
}
