using Wolfgang.Barefoot.Contracts.Runs;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Runs;

public sealed class RunMonitorRecordsTests
{
    private static readonly Guid First = Guid.Parse("5d0c6f0e-3a51-4c1d-8a43-1b2c3d4e5f60");

    private static readonly Guid Second = Guid.Parse("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");

    private static readonly DateTimeOffset Started = new(2026, 11, 6, 6, 0, 0, TimeSpan.Zero);



    [Fact]
    public void WorkProgress_when_every_value_is_given_keeps_each_value()
    {
        var progress = new WorkProgress(132, 212, 2);

        Assert.Equal(132, progress.DoneCount);
        Assert.Equal(212, progress.TotalCount);
        Assert.Equal(2, progress.FailedCount);
    }



    [Fact]
    public void SearchProgressResponse_when_every_value_is_given_keeps_each_value()
    {
        var lookups = new WorkProgress(31, 34, 0);
        var search = new SearchProgressResponse(First, "Late April", "Alex Example", SearchRunStage.Exploded, lookups, 50);

        Assert.Equal(First, search.SearchId);
        Assert.Equal("Late April", search.SearchName);
        Assert.Equal("Alex Example", search.OwnerName);
        Assert.Equal(SearchRunStage.Exploded, search.Stage);
        Assert.Equal(lookups, search.Lookups);
        Assert.Equal(50, search.RunOrder);
    }



    [Fact]
    public void RunProgressResponse_when_a_worker_is_claiming_keeps_each_value()
    {
        var lookups = new WorkProgress(132, 212, 2);
        var flightSets = new WorkProgress(0, 38, 0);
        var search = new SearchProgressResponse(Second, "Late April", "Alex Example", SearchRunStage.Exploded, new WorkProgress(31, 34, 0), 50);
        var run = new RunProgressResponse
        (
            First,
            "scheduled",
            Started,
            "scraper-1",
            Started.AddMinutes(26),
            lookups,
            flightSets,
            [search]
        );

        Assert.Equal(First, run.Id);
        Assert.Equal("scheduled", run.Trigger);
        Assert.Equal(Started, run.StartedUtc);
        Assert.Equal("scraper-1", run.WorkerName);
        Assert.Equal(Started.AddMinutes(26), run.LastClaimUtc);
        Assert.Equal(lookups, run.Lookups);
        Assert.Equal(flightSets, run.FlightSets);
        Assert.Equal(search, Assert.Single(run.Searches));
    }



    [Fact]
    public void RunProgressResponse_when_nothing_has_been_claimed_names_no_worker_and_no_claim()
    {
        var run = new RunProgressResponse(First, "manual", Started, null, null, new WorkProgress(0, 212, 0), new WorkProgress(0, 38, 0), []);

        Assert.Null(run.WorkerName);
        Assert.Null(run.LastClaimUtc);
        Assert.Empty(run.Searches);
    }



    [Fact]
    public void RunHistoryResponse_when_a_run_was_resumed_after_stopping_early_keeps_each_value()
    {
        var lookups = new WorkProgress(208, 208, 14);
        var flightSets = new WorkProgress(36, 38, 0);
        var run = new RunHistoryResponse
        (
            First,
            "scheduled",
            Started,
            Started.AddHours(3),
            RunStatus.Incomplete,
            40,
            lookups,
            flightSets,
            11,
            1,
            9.8m,
            "Five timeouts in a row on the room page."
        );

        Assert.Equal(First, run.Id);
        Assert.Equal("scheduled", run.Trigger);
        Assert.Equal(Started, run.StartedUtc);
        Assert.Equal(Started.AddHours(3), run.ResumedUtc);
        Assert.Equal(RunStatus.Incomplete, run.Status);
        Assert.Equal(40, run.DurationMinutes);
        Assert.Equal(lookups, run.Lookups);
        Assert.Equal(flightSets, run.FlightSets);
        Assert.Equal(11, run.SentDigestCount);
        Assert.Equal(1, run.FailedDigestCount);
        Assert.Equal(9.8m, run.SecondsPerLookup);
        Assert.Equal("Five timeouts in a row on the room page.", run.Note);
    }



    [Fact]
    public void RunHistoryResponse_when_a_run_did_nothing_has_no_resume_no_duration_no_pace_and_no_note()
    {
        var run = new RunHistoryResponse
        (
            First,
            "scheduled",
            Started,
            null,
            RunStatus.Failed,
            null,
            new WorkProgress(0, 204, 0),
            new WorkProgress(0, 38, 0),
            0,
            0,
            null,
            null
        );

        Assert.Null(run.ResumedUtc);
        Assert.Null(run.DurationMinutes);
        Assert.Null(run.SecondsPerLookup);
        Assert.Null(run.Note);
    }
}
