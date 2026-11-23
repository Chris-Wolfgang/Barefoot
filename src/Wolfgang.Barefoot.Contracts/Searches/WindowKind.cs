namespace Wolfgang.Barefoot.Contracts.Searches;

/// <summary>
/// Which of the two kinds of check-in window a search definition describes.
/// </summary>
public enum WindowKind
{
    /// <summary>No kind was chosen. It is what a value nobody set holds, and no search is valid with it.</summary>
    NotSet = 0,

    /// <summary>The same days every year, given as month and day.</summary>
    Recurring = 1,

    /// <summary>One occasion, given as explicit dates.</summary>
    OneTime = 2,
}
