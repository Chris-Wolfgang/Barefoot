namespace Wolfgang.Barefoot.Contracts.Searches;

/// <summary>
/// The resort brand a search is for. A search is for one brand, because the brands differ in who may
/// travel (DESIGN §4).
/// </summary>
public enum Brand
{
    /// <summary>No brand was chosen. It is what a value nobody set holds, and no search is valid with it.</summary>
    NotSet = 0,

    /// <summary>Adults only: a search has no children and no infants.</summary>
    Sandals = 1,

    /// <summary>Families: a search may include children and infants.</summary>
    Beaches = 2,
}
