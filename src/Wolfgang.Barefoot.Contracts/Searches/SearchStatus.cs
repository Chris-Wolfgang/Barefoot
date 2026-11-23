namespace Wolfgang.Barefoot.Contracts.Searches;

/// <summary>
/// Where a search is in its life (DESIGN §4): Draft → Active → Inactive.
/// </summary>
public enum SearchStatus
{
    /// <summary>Being written. It does not run, and it can be deleted as long as it never ran.</summary>
    Draft = 0,

    /// <summary>It runs with every run.</summary>
    Active = 1,

    /// <summary>It no longer runs. Its results are kept.</summary>
    Inactive = 2,
}
