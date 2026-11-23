namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// Whether a resort can be booked during a <see cref="ResortSpan"/>.
/// </summary>
public enum ResortSpanStatus
{
    /// <summary>No status was given. It is what a value nobody set holds, and no span is stored with it.</summary>
    NotSet = 0,

    /// <summary>The site lists the resort.</summary>
    Online = 1,

    /// <summary>The site does not list the resort: it is closed, or not yet open.</summary>
    Offline = 2,
}
