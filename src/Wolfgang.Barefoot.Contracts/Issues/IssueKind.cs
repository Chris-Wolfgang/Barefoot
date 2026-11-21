namespace Wolfgang.Barefoot.Contracts.Issues;

/// <summary>
/// What an Open issue is about (DESIGN §12, "First sign-in and attention"). The kind decides what
/// resolves the issue and which action the screen offers.
/// </summary>
public enum IssueKind
{
    /// <summary>A digest could not be delivered. Resolved when a delivery succeeds.</summary>
    DeliveryFailed = 0,

    /// <summary>A one-time check-in window passed and its search deactivated itself. Resolved when a new window is set.</summary>
    WindowPassed = 1,

    /// <summary>A resort of one of the person's searches is offline on the booking site. Resolved when the site lists it again.</summary>
    ResortOffline = 2,

    /// <summary>Admin only: deliveries failed for every user. Resolved when a delivery succeeds.</summary>
    AllDeliveriesFailed = 3,

    /// <summary>Admin only: an airport has no time zone. Resolved when one is set.</summary>
    AirportWithoutTimeZone = 4,
}
