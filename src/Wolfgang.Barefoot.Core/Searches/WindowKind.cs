namespace Wolfgang.Barefoot.Core.Searches;

/// <summary>
/// Which of the two kinds of check-in window a search definition describes.
/// </summary>
public enum WindowKind
{
    /// <summary>The same days every year, given as month and day.</summary>
    Recurring = 0,

    /// <summary>One occasion, given as explicit dates.</summary>
    OneTime = 1,
}
