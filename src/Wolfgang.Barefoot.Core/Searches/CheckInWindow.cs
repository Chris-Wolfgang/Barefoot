namespace Wolfgang.Barefoot.Core.Searches;

/// <summary>
/// The dates a search may check in on. A window is either recurring or one-time, never both: the two
/// kinds are separate types, so a search cannot hold fields of both (DESIGN §4).
/// </summary>
public abstract record CheckInWindow
{
    private protected CheckInWindow()
    {
    }
}
