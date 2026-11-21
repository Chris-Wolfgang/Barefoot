namespace Wolfgang.Barefoot.Contracts.Digests;

/// <summary>
/// Where one digest delivery stands (ADR 0006 §1).
/// </summary>
public enum DeliveryStatus
{
    /// <summary>Stored and not yet sent.</summary>
    Pending = 0,

    /// <summary>Sent.</summary>
    Sent = 1,

    /// <summary>Sending was tried and failed.</summary>
    Failed = 2,
}
