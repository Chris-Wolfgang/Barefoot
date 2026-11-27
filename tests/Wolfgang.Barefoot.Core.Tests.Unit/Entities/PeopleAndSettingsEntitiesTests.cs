using Wolfgang.Barefoot.Contracts.Issues;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Entities;

public sealed class PeopleAndSettingsEntitiesTests
{
    private static readonly Guid Oid = Guid.Parse("7b1c2d3e-4f50-4a61-9b72-8c93a4b5c6d7");

    private static readonly Guid Id = Guid.Parse("1a2b3c4d-5e6f-4071-8293-a4b5c6d7e8f9");

    private static readonly Guid Target = Guid.Parse("9f8e7d6c-5b4a-4392-8170-6f5e4d3c2b1a");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, TimeSpan.Zero);



    [Fact]
    public void AppUser_when_every_value_is_given_keeps_each_value()
    {
        var user = new AppUser
        {
            Oid = Oid,
            DisplayName = "Alex Example",
            Email = "alex@example.com",
            LastSeenUtc = Instant,
            DisclaimerVersionAccepted = 3,
            DisclaimerAcceptedUtc = Instant.AddDays(-30),
            TimeZoneId = "America/New_York",
            DefaultAdultCount = 2,
            DefaultChildCount = 1,
            DefaultInfantCount = 1,
            SessionsValidAfterUtc = Instant.AddDays(-1),
            Version = 4,
        };

        Assert.Equal(Oid, user.Oid);
        Assert.Equal("Alex Example", user.DisplayName);
        Assert.Equal("alex@example.com", user.Email);
        Assert.Equal(Instant, user.LastSeenUtc);
        Assert.Equal(3, user.DisclaimerVersionAccepted);
        Assert.Equal(Instant.AddDays(-30), user.DisclaimerAcceptedUtc);
        Assert.Equal("America/New_York", user.TimeZoneId);
        Assert.Equal(2, user.DefaultAdultCount);
        Assert.Equal(1, user.DefaultChildCount);
        Assert.Equal(1, user.DefaultInfantCount);
        Assert.Equal(Instant.AddDays(-1), user.SessionsValidAfterUtc);
        Assert.Equal(4, user.Version);
    }



    [Fact]
    public void AppUser_when_only_the_required_values_are_given_has_accepted_nothing_and_chosen_no_zone()
    {
        var user = new AppUser { Oid = Oid, DisplayName = "Alex Example", Email = "alex@example.com" };

        Assert.Null(user.DisclaimerVersionAccepted);
        Assert.Null(user.DisclaimerAcceptedUtc);
        Assert.Null(user.TimeZoneId);
        Assert.Null(user.SessionsValidAfterUtc);
        Assert.Equal(0, user.Version);
    }



    [Fact]
    public void AppUser_when_asked_for_the_unassigned_user_gives_the_empty_guid_and_its_name()
    {
        Assert.Equal(Guid.Empty, AppUser.UnassignedOid);
        Assert.Equal("Unassigned", AppUser.UnassignedDisplayName);
    }



    [Fact]
    public void Setting_when_every_value_is_given_keeps_each_value()
    {
        var setting = new Setting
        {
            Key = "vocab.stale_after_runs",
            Value = "6",
            UpdatedUtc = Instant,
            UpdatedByUserId = Oid,
            Version = 2,
        };

        Assert.Equal("vocab.stale_after_runs", setting.Key);
        Assert.Equal("6", setting.Value);
        Assert.Equal(Instant, setting.UpdatedUtc);
        Assert.Equal(Oid, setting.UpdatedByUserId);
        Assert.Equal(2, setting.Version);
    }



    [Fact]
    public void Recipient_when_every_value_is_given_keeps_each_value()
    {
        var recipient = new Recipient
        {
            Id = Id,
            OwnerId = Oid,
            Address = "sam@example.com",
            DisplayName = "Sam",
            Version = 1,
        };

        Assert.Equal(Id, recipient.Id);
        Assert.Equal(Oid, recipient.OwnerId);
        Assert.Equal("sam@example.com", recipient.Address);
        Assert.Equal("Sam", recipient.DisplayName);
        Assert.Equal(1, recipient.Version);
    }



    [Fact]
    public void OpenIssue_when_every_value_is_given_keeps_each_value()
    {
        var issue = new OpenIssue
        {
            Id = Id,
            UserId = Oid,
            Kind = IssueKind.ResortOffline,
            Subject = "Halcyon Beach is offline on the booking site.",
            ResolvesWhen = "Goes away when the site lists it again.",
            TargetId = Target,
            RaisedUtc = Instant,
            AcknowledgedUtc = Instant.AddHours(2),
            ResolvedUtc = Instant.AddDays(3),
        };

        Assert.Equal(Id, issue.Id);
        Assert.Equal(Oid, issue.UserId);
        Assert.Equal(IssueKind.ResortOffline, issue.Kind);
        Assert.Equal("Halcyon Beach is offline on the booking site.", issue.Subject);
        Assert.Equal("Goes away when the site lists it again.", issue.ResolvesWhen);
        Assert.Equal(Target, issue.TargetId);
        Assert.Equal(Instant, issue.RaisedUtc);
        Assert.Equal(Instant.AddHours(2), issue.AcknowledgedUtc);
        Assert.Equal(Instant.AddDays(3), issue.ResolvedUtc);
    }



    [Fact]
    public void OpenIssue_when_it_is_new_and_about_no_single_thing_has_no_target_and_is_neither_acknowledged_nor_resolved()
    {
        var issue = new OpenIssue
        {
            Id = Id,
            UserId = Oid,
            Kind = IssueKind.AllDeliveriesFailed,
            Subject = "No digest could be delivered after the last run.",
            ResolvesWhen = "Goes away when a delivery succeeds.",
        };

        Assert.Null(issue.TargetId);
        Assert.Null(issue.AcknowledgedUtc);
        Assert.Null(issue.ResolvedUtc);
    }
}
