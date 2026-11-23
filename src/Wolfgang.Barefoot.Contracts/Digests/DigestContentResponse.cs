namespace Wolfgang.Barefoot.Contracts.Digests;

/// <summary>
/// The stored body of a digest, exactly as it was or will be sent (ADR 0006 §1).
/// </summary>
/// <param name="Id">The delivery's id.</param>
/// <param name="Subject">The subject line.</param>
/// <param name="HtmlBody">The HTML part.</param>
/// <param name="TextBody">The plain-text part.</param>
public sealed record DigestContentResponse
(
    Guid Id,
    string Subject,
    string HtmlBody,
    string TextBody
);
