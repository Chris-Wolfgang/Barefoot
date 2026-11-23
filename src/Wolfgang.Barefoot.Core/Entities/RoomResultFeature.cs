namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One feature a room result has. A result names a feature at most once.
/// </summary>
public sealed class RoomResultFeature
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="RoomResult"/> that has the feature.</summary>
    public required Guid RoomResultId { get; set; }



    /// <summary>The feature, a <see cref="VocabularyValue"/>.</summary>
    public required Guid VocabularyValueId { get; set; }
}
