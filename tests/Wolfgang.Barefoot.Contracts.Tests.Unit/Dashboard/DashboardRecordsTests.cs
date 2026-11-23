using Wolfgang.Barefoot.Contracts.Dashboard;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Dashboard;

public sealed class DashboardRecordsTests
{
    private static readonly Guid First = Guid.Parse("5d0c6f0e-3a51-4c1d-8a43-1b2c3d4e5f60");

    private static readonly Guid Second = Guid.Parse("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");

    private static readonly DateTimeOffset Started = new(2026, 11, 3, 6, 12, 0, TimeSpan.Zero);



    [Theory]
    [InlineData(RecentResultStatus.NotSet, 0)]
    [InlineData(RecentResultStatus.Queued, 1)]
    [InlineData(RecentResultStatus.InProgress, 2)]
    [InlineData(RecentResultStatus.Complete, 3)]
    [InlineData(RecentResultStatus.Incomplete, 4)]
    [InlineData(RecentResultStatus.Failed, 5)]
    [InlineData(RecentResultStatus.Skipped, 6)]
    public void RecentResultStatus_when_cast_to_a_number_keeps_its_declared_value(RecentResultStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }



    [Fact]
    public void RecentResultResponse_when_a_search_finished_with_failures_keeps_each_value()
    {
        var row = new RecentResultResponse
        (
            First,
            "Late April",
            10,
            2,
            1,
            1,
            true,
            Second,
            Started,
            55,
            RecentResultStatus.Incomplete,
            32,
            32,
            2,
            "2 failed",
            9840m,
            120m,
            Started.AddMinutes(28)
        );

        Assert.Equal(First, row.SearchId);
        Assert.Equal("Late April", row.SearchName);
        Assert.Equal(10, row.NightCount);
        Assert.Equal(2, row.AdultCount);
        Assert.Equal(1, row.ChildCount);
        Assert.Equal(1, row.InfantCount);
        Assert.True(row.IncludeFlights);
        Assert.Equal(Second, row.SearchRunId);
        Assert.Equal(Started, row.RunStartedUtc);
        Assert.Equal(55, row.DurationMinutes);
        Assert.Equal(RecentResultStatus.Incomplete, row.Status);
        Assert.Equal(32, row.DoneLookupCount);
        Assert.Equal(32, row.LookupCount);
        Assert.Equal(2, row.FailedLookupCount);
        Assert.Equal("2 failed", row.Note);
        Assert.Equal(9840m, row.CheapestTotalAmount);
        Assert.Equal(120m, row.ChangeAmount);
        Assert.Equal(Started.AddMinutes(28), row.DigestSentUtc);
    }



    [Fact]
    public void RecentResultResponse_when_the_run_left_the_search_out_has_no_run_part_no_prices_and_no_digest()
    {
        var row = new RecentResultResponse
        (
            First,
            "Anniversary",
            7,
            2,
            0,
            0,
            false,
            null,
            Started,
            null,
            RecentResultStatus.Skipped,
            0,
            0,
            0,
            "window passed",
            null,
            null,
            null
        );

        Assert.Null(row.SearchRunId);
        Assert.Null(row.DurationMinutes);
        Assert.Null(row.CheapestTotalAmount);
        Assert.Null(row.ChangeAmount);
        Assert.Null(row.DigestSentUtc);
        Assert.False(row.IncludeFlights);
    }



    [Fact]
    public void PriceMoverResponse_when_a_price_dropped_keeps_each_value()
    {
        var mover = new PriceMoverResponse(First, "Halcyon Beach", "Beachfront Butler", new DateOnly(2027, 2, 22), 7, -486m);

        Assert.Equal(First, mover.SearchRunId);
        Assert.Equal("Halcyon Beach", mover.ResortName);
        Assert.Equal("Beachfront Butler", mover.RoomName);
        Assert.Equal(new DateOnly(2027, 2, 22), mover.CheckIn);
        Assert.Equal(7, mover.NightCount);
        Assert.Equal(-486m, mover.ChangeAmount);
    }



    [Fact]
    public void LatestDigestResponse_when_rooms_matched_keeps_each_value()
    {
        var digest = new LatestDigestResponse(First, Second, "February week", Started, 38, 7212m, 2);

        Assert.Equal(First, digest.DigestId);
        Assert.Equal(Second, digest.SearchRunId);
        Assert.Equal("February week", digest.SearchName);
        Assert.Equal(Started, digest.SentUtc);
        Assert.Equal(38, digest.MatchCount);
        Assert.Equal(7212m, digest.CheapestTotalAmount);
        Assert.Equal(2, digest.ResortCount);
    }



    [Fact]
    public void LatestDigestResponse_when_nothing_matched_has_no_cheapest()
    {
        var digest = new LatestDigestResponse(First, Second, "February week", Started, 0, null, 0);

        Assert.Null(digest.CheapestTotalAmount);
    }



    [Fact]
    public void DashboardResponse_when_a_person_has_runs_and_a_digest_keeps_each_value()
    {
        var mover = new PriceMoverResponse(First, "Halcyon Beach", "Beachfront Butler", new DateOnly(2027, 2, 22), 7, -486m);
        var digest = new LatestDigestResponse(First, Second, "February week", Started.AddMinutes(28), 38, 7212m, 2);
        var dashboard = new DashboardResponse
        (
            3,
            Started,
            Started.AddDays(3),
            Started.AddMinutes(28),
            Started.AddDays(-3),
            [mover],
            digest
        );

        Assert.Equal(3, dashboard.ActiveSearchCount);
        Assert.Equal(Started, dashboard.LastSearchedUtc);
        Assert.Equal(Started.AddDays(3), dashboard.NextRunUtc);
        Assert.Equal(Started.AddMinutes(28), dashboard.LastDigestUtc);
        Assert.Equal(Started.AddDays(-3), dashboard.MoversSinceUtc);
        Assert.Equal(mover, Assert.Single(dashboard.Movers));
        Assert.Equal(digest, dashboard.LatestDigest);
    }



    [Fact]
    public void DashboardResponse_when_a_person_has_just_signed_in_has_no_searches_no_instants_and_no_digest()
    {
        var dashboard = new DashboardResponse(0, null, null, null, null, [], null);

        Assert.Equal(0, dashboard.ActiveSearchCount);
        Assert.Null(dashboard.LastSearchedUtc);
        Assert.Null(dashboard.NextRunUtc);
        Assert.Null(dashboard.LastDigestUtc);
        Assert.Null(dashboard.MoversSinceUtc);
        Assert.Empty(dashboard.Movers);
        Assert.Null(dashboard.LatestDigest);
    }
}
