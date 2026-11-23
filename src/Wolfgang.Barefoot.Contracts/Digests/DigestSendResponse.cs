namespace Wolfgang.Barefoot.Contracts.Digests;

/// <summary>
/// What <c>POST /runs/{id}/digests/send</c> returns once every pending delivery has been tried
/// (ADR 0006 §1). A repeat finds nothing pending and reports zero for both.
/// </summary>
/// <param name="SentCount">How many deliveries were sent.</param>
/// <param name="FailedCount">How many deliveries failed.</param>
public sealed record DigestSendResponse(int SentCount, int FailedCount);
