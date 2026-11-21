namespace Wolfgang.Barefoot.Contracts.Catalog;

/// <summary>
/// The four room vocabularies (DESIGN §7). Each is discovered from results and is one of a search's
/// digest filters.
/// </summary>
public enum VocabularyKind
{
    /// <summary>A room level, for example Club or Butler.</summary>
    RoomLevel = 0,

    /// <summary>A room view, for example Oceanview.</summary>
    RoomView = 1,

    /// <summary>A room type.</summary>
    RoomType = 2,

    /// <summary>A room feature, for example a private pool.</summary>
    RoomFeature = 3,
}
