namespace Wolfgang.Barefoot.Contracts.Searches;

/// <summary>
/// The resort brand a search is for. A search is for one brand, because the brands differ in who may
/// travel (DESIGN §4).
/// </summary>
public enum Brand
{
    /// <summary>Adults only: a search has no children and no infants.</summary>
    Sandals = 0,

    /// <summary>Families: a search may include children and infants.</summary>
    Beaches = 1,
}
