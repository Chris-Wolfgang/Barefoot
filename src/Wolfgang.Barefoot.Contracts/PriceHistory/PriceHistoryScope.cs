namespace Wolfgang.Barefoot.Contracts.PriceHistory;

/// <summary>
/// Whose captures a price history is built from (DESIGN §12, price history scoping).
/// </summary>
public enum PriceHistoryScope
{
    /// <summary>Only the captures of the caller's own searches. The default, and all an Editor may ask for.</summary>
    Mine = 0,

    /// <summary>The captures of every user's searches. Only an Admin may ask for it.</summary>
    AllUsers = 1,
}
