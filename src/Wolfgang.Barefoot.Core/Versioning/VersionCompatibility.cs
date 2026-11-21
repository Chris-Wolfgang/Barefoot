namespace Wolfgang.Barefoot.Core.Versioning;

/// <summary>
/// Whether a client can work with the API it reached (ADR 0010 §8).
/// </summary>
public enum VersionCompatibility
{
    /// <summary>The client and the API agree on the contract.</summary>
    Compatible = 0,

    /// <summary>The API's contract revision is below the client's: the API must be deployed.</summary>
    ApiTooOld = 1,

    /// <summary>The API's contract revision is above the client's: the client must be updated.</summary>
    ApiTooNew = 2,
}
