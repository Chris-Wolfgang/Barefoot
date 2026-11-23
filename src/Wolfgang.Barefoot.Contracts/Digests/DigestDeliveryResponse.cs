namespace Wolfgang.Barefoot.Contracts.Digests;

/// <summary>
/// One digest sent, or to be sent, to one address: a row of the digest lists (ADR 0006 §5). The
/// body is not included; it is read separately.
/// </summary>
/// <param name="Id">The delivery's id.</param>
/// <param name="SearchId">The search the digest is for.</param>
/// <param name="RunId">The run the digest reports.</param>
/// <param name="RecipientAddress">The address, as it was when the digest was stored.</param>
/// <param name="Subject">The subject line.</param>
/// <param name="Status">Where the delivery stands.</param>
/// <param name="Attempts">How many times sending was tried, resends included.</param>
/// <param name="CreatedUtc">When the digest was stored.</param>
/// <param name="SentUtc">When it was last sent, or <see langword="null"/> when it never was.</param>
public sealed record DigestDeliveryResponse
(
    Guid Id,
    Guid SearchId,
    Guid RunId,
    string RecipientAddress,
    string Subject,
    DeliveryStatus Status,
    int Attempts,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? SentUtc
);
