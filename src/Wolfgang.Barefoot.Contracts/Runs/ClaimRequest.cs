namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// The body of the two <c>claim</c> calls (DESIGN §5). It takes no lease length: the API sets every lease.
/// </summary>
/// <param name="Worker">The name of the worker that claims, stored on the row it is leased.</param>
public sealed record ClaimRequest(string Worker);
