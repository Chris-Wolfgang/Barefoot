namespace Wolfgang.Barefoot.Contracts.SystemStatus;

/// <summary>
/// Whether the serverless database is awake, as the Admin's System page shows it (DESIGN §12, Budget
/// and health). A database that is online at an odd hour has a session holding it awake.
/// </summary>
public enum DatabaseStatus
{
    /// <summary>The status is not known. It is what a value nobody set holds.</summary>
    NotSet = 0,

    /// <summary>The database is awake and using its allowance.</summary>
    Online = 1,

    /// <summary>The database is paused and costs nothing.</summary>
    Paused = 2,
}
