namespace Wolfgang.Barefoot.Core.Keys;

/// <summary>
/// How serious an <see cref="ErrorCode"/> is for the person or program that receives it.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>Nothing is wrong; the caller should wait or try again, for example while the database wakes.</summary>
    Information = 0,

    /// <summary>The request was refused for a reason the caller can correct.</summary>
    Warning = 1,

    /// <summary>The request failed and the caller cannot correct it.</summary>
    Error = 2,
}
