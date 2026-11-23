namespace Wolfgang.Barefoot.Core.Digests;

/// <summary>
/// A digest ready to store and send: the stored bodies are exactly what is mailed (ADR 0006 §1).
/// </summary>
/// <param name="Subject">The subject line.</param>
/// <param name="HtmlBody">The HTML part.</param>
/// <param name="TextBody">The plain-text part, which is always present.</param>
public sealed record RenderedDigest(string Subject, string HtmlBody, string TextBody);
