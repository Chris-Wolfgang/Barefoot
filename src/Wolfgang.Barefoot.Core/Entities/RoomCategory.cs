namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One kind of room a resort sells, as results reveal it (DESIGN §7). A price is followed over time
/// by resort, check-in date and room category.
/// </summary>
public sealed class RoomCategory
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="Resort"/> that sells the room.</summary>
    public required Guid ResortId { get; set; }



    /// <summary>The site's code for the room, unique within the resort.</summary>
    public required string Code { get; set; }



    /// <summary>The room's name as the site gives it.</summary>
    public required string Name { get; set; }



    /// <summary>When a result first showed the room.</summary>
    public DateTimeOffset FirstSeenUtc { get; set; }



    /// <summary>When a result last showed the room.</summary>
    public DateTimeOffset LastSeenUtc { get; set; }
}
