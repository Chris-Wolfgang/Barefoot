using Wolfgang.Barefoot.Contracts.Users;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Users;

public sealed class UserRecordsTests
{
    private static readonly Guid Id = Guid.Parse("5d0c6f0e-3a51-4c1d-8a43-1b2c3d4e5f60");

    private static readonly DateTimeOffset Seen = new(2026, 11, 21, 8, 12, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Accepted = new(2026, 11, 6, 19, 30, 0, TimeSpan.Zero);



    [Theory]
    [InlineData(UserRole.Viewer, 0)]
    [InlineData(UserRole.Editor, 1)]
    [InlineData(UserRole.Admin, 2)]
    public void UserRole_when_cast_to_a_number_keeps_its_declared_value(UserRole role, int expected)
    {
        Assert.Equal(expected, (int)role);
    }



    [Fact]
    public void UserResponse_when_a_person_has_accepted_the_terms_keeps_each_value_and_can_own_a_search()
    {
        var user = new UserResponse
        (
            Id,
            "Example Person",
            "person@example.com",
            UserRole.Editor,
            3,
            5,
            Seen,
            1,
            Accepted,
            CanOwnSearch: true,
            "America/New_York",
            2,
            1,
            1,
            Seen.AddDays(-1),
            7
        );

        Assert.Equal(Id, user.Id);
        Assert.Equal("Example Person", user.DisplayName);
        Assert.Equal("person@example.com", user.Email);
        Assert.Equal(UserRole.Editor, user.Role);
        Assert.Equal(3, user.ActiveSearchCount);
        Assert.Equal(5, user.ActiveSearchLimit);
        Assert.Equal(Seen, user.LastSeenUtc);
        Assert.Equal(1, user.DisclaimerVersionAccepted);
        Assert.Equal(Accepted, user.DisclaimerAcceptedUtc);
        Assert.Equal("America/New_York", user.TimeZoneId);
        Assert.Equal(2, user.DefaultAdults);
        Assert.Equal(1, user.DefaultChildren);
        Assert.Equal(1, user.DefaultInfants);
        Assert.Equal(Seen.AddDays(-1), user.SessionsValidAfterUtc);
        Assert.Equal(7, user.Version);
        Assert.True(user.CanOwnSearch);
    }



    [Fact]
    public void UserResponse_when_a_person_has_not_accepted_the_terms_cannot_own_a_search()
    {
        var user = new UserResponse(Id, "Example Person", "person@example.com", UserRole.Viewer, 0, 5, Seen, null, null, CanOwnSearch: false, null, 2, 0, 0, null, 1);

        Assert.Null(user.DisclaimerVersionAccepted);
        Assert.Null(user.DisclaimerAcceptedUtc);
        Assert.Null(user.TimeZoneId);
        Assert.Null(user.SessionsValidAfterUtc);
        Assert.False(user.CanOwnSearch);
    }



    [Fact]
    public void UpdateProfileRequest_when_only_the_adults_are_given_leaves_the_young_travellers_at_none()
    {
        var request = new UpdateProfileRequest(null, 2);

        Assert.Null(request.TimeZoneId);
        Assert.Equal(2, request.DefaultAdults);
        Assert.Equal(0, request.DefaultChildren);
        Assert.Equal(0, request.DefaultInfants);
    }



    [Fact]
    public void UpdateProfileRequest_when_every_value_is_given_keeps_each_one()
    {
        var request = new UpdateProfileRequest("Europe/London", 2, 3, 1);

        Assert.Equal("Europe/London", request.TimeZoneId);
        Assert.Equal(3, request.DefaultChildren);
        Assert.Equal(1, request.DefaultInfants);
    }



    [Fact]
    public void AcceptDisclaimerRequest_when_built_keeps_the_version()
    {
        Assert.Equal(2, new AcceptDisclaimerRequest(2).Version);
    }



    [Fact]
    public void UserFilter_when_nothing_is_given_narrows_nothing()
    {
        var filter = new UserFilter();

        Assert.Null(filter.Find);
        Assert.False(filter.CanOwnSearchOnly);
    }



    [Fact]
    public void UserFilter_when_every_value_is_given_keeps_each_one()
    {
        var filter = new UserFilter("exam", CanOwnSearchOnly: true);

        Assert.Equal("exam", filter.Find);
        Assert.True(filter.CanOwnSearchOnly);
    }
}
