using Wolfgang.Barefoot.Contracts.Digests;

namespace Wolfgang.Barefoot.Core.Entities;

/// <summary>
/// One digest for one recipient (ADR 0006). The digest is built and stored when a search's run
/// completes, and sent later from the stored row; a resend mails the same body again.
/// </summary>
public sealed class DigestDelivery
{
    /// <summary>The row's key.</summary>
    public required Guid Id { get; set; }



    /// <summary>The <see cref="MatrixRun"/> the digest reports on.</summary>
    public required Guid MatrixRunId { get; set; }



    /// <summary>
    /// The <see cref="AppUser"/> the digest is for, or <see langword="null"/> when it goes to an extra
    /// recipient who is not a user.
    /// </summary>
    public Guid? RecipientUserId { get; set; }



    /// <summary>
    /// The address the digest is mailed to, copied when the digest was built, so a later change to
    /// the address book does not change where this one went.
    /// </summary>
    public required string RecipientAddress { get; set; }



    /// <summary>The subject line.</summary>
    public required string Subject { get; set; }



    /// <summary>The HTML part.</summary>
    public required string HtmlBody { get; set; }



    /// <summary>The plain-text part.</summary>
    public required string TextBody { get; set; }



    /// <summary>Whether the digest waits to be sent, was sent, or failed. A new digest waits.</summary>
    public DeliveryStatus Status { get; set; }



    /// <summary>How many times sending it has been tried.</summary>
    public int Attempts { get; set; }



    /// <summary>The error the last failed try reported, or <see langword="null"/>.</summary>
    public string? LastError { get; set; }



    /// <summary>When the digest was built.</summary>
    public DateTimeOffset CreatedUtc { get; set; }



    /// <summary>When it was sent, or <see langword="null"/> while it has not been.</summary>
    public DateTimeOffset? SentUtc { get; set; }
}
