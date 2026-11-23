namespace Wolfgang.Barefoot.Contracts.SystemStatus;

/// <summary>
/// The Admin page that puts right one item of the System page's "Needs attention" list.
/// </summary>
public enum AttentionTarget
{
    /// <summary>No page was given. It is what a value nobody set holds, and the API never reports it.</summary>
    NotSet = 0,

    /// <summary>The Deliveries page, where a failed digest is resent.</summary>
    Deliveries = 1,

    /// <summary>The Reference data page, where an offline resort or an airport without a time zone is seen to.</summary>
    ReferenceData = 2,

    /// <summary>The Users page, where a person's searches and limits are seen.</summary>
    Users = 3,

    /// <summary>The Runs page, where an incomplete run is seen.</summary>
    Runs = 4,
}
