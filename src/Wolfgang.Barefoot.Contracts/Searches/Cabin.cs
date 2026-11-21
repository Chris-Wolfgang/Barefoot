namespace Wolfgang.Barefoot.Contracts.Searches;

/// <summary>
/// The cabin flights are looked up in. It affects flights only, never the room lookup.
/// </summary>
public enum Cabin
{
    /// <summary>Economy.</summary>
    Economy = 0,

    /// <summary>Premium economy.</summary>
    PremiumEconomy = 1,

    /// <summary>Business.</summary>
    Business = 2,

    /// <summary>First.</summary>
    First = 3,
}
