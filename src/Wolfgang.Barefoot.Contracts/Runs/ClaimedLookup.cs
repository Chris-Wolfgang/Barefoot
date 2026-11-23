namespace Wolfgang.Barefoot.Contracts.Runs;

/// <summary>
/// One room lookup leased to a worker: what to look up, and the token its result must carry (ADR 0003).
/// </summary>
/// <param name="RequestId">The request's id.</param>
/// <param name="Attempt">The attempt token minted by this claim.</param>
/// <param name="LeaseExpiresUtc">When the lease ends; after it the request returns to the others.</param>
/// <param name="ResortCode">The resort's code on the booking site.</param>
/// <param name="CheckIn">The check-in date.</param>
/// <param name="CheckOut">The check-out date.</param>
/// <param name="AdultCount">The number of adults.</param>
/// <param name="ChildCount">The number of children.</param>
/// <param name="InfantCount">The number of infants.</param>
public sealed record ClaimedLookup
(
    Guid RequestId,
    Guid Attempt,
    DateTimeOffset LeaseExpiresUtc,
    string ResortCode,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int AdultCount,
    int ChildCount,
    int InfantCount
);
