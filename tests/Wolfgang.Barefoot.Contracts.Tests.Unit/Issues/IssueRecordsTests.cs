using Wolfgang.Barefoot.Contracts.Issues;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Issues;

public sealed class IssueRecordsTests
{
    private static readonly Guid Id = Guid.Parse("5d0c6f0e-3a51-4c1d-8a43-1b2c3d4e5f60");

    private static readonly Guid Target = Guid.Parse("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");

    private static readonly DateTimeOffset Raised = new(2026, 11, 4, 6, 40, 0, TimeSpan.Zero);



    [Theory]
    [InlineData(IssueKind.DeliveryFailed, 0)]
    [InlineData(IssueKind.WindowPassed, 1)]
    [InlineData(IssueKind.ResortOffline, 2)]
    [InlineData(IssueKind.AllDeliveriesFailed, 3)]
    [InlineData(IssueKind.AirportWithoutTimeZone, 4)]
    public void IssueKind_when_cast_to_a_number_keeps_its_declared_value(IssueKind kind, int expected)
    {
        Assert.Equal(expected, (int)kind);
    }



    [Fact]
    public void OpenIssueResponse_when_an_issue_is_acknowledged_keeps_each_value()
    {
        var issue = new OpenIssueResponse
        (
            Id,
            IssueKind.ResortOffline,
            "Halcyon Beach is offline on the booking site.",
            "Goes away when the site lists it again.",
            Target,
            Raised,
            Raised.AddHours(2)
        );

        Assert.Equal(Id, issue.Id);
        Assert.Equal(IssueKind.ResortOffline, issue.Kind);
        Assert.Equal("Halcyon Beach is offline on the booking site.", issue.Subject);
        Assert.Equal("Goes away when the site lists it again.", issue.ResolvesWhen);
        Assert.Equal(Target, issue.TargetId);
        Assert.Equal(Raised, issue.RaisedUtc);
        Assert.Equal(Raised.AddHours(2), issue.AcknowledgedUtc);
    }



    [Fact]
    public void OpenIssueResponse_when_an_issue_is_new_and_about_no_single_thing_has_no_target_and_no_acknowledgement()
    {
        var issue = new OpenIssueResponse
        (
            Id,
            IssueKind.AllDeliveriesFailed,
            "No digest could be delivered after the last run.",
            "Goes away when a delivery succeeds.",
            null,
            Raised,
            null
        );

        Assert.Null(issue.TargetId);
        Assert.Null(issue.AcknowledgedUtc);
    }
}
