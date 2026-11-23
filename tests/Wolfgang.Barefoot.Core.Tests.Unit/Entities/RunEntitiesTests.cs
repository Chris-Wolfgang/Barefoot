using Wolfgang.Barefoot.Contracts.Runs;
using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Core.Tests.Unit.Entities;

public sealed class RunEntitiesTests
{
    private static readonly Guid Id = Guid.Parse("4d5e6f70-8192-43a4-b5c6-d7e8f9012a3b");

    private static readonly Guid Parent = Guid.Parse("5c4b3a29-1807-4f6e-9d5c-4b3a2918071f");

    private static readonly Guid Other = Guid.Parse("0f1e2d3c-4b5a-4697-8879-6a5b4c3d2e1f");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, TimeSpan.Zero);



    [Fact]
    public void Run_when_it_was_aborted_and_finished_keeps_each_value()
    {
        var run = new Run
        {
            Id = Id,
            Trigger = "scheduled",
            Status = RunStatus.Incomplete,
            StartedUtc = Instant,
            FinishedUtc = Instant.AddHours(1),
            AbortReason = "The site refused five lookups in a row.",
        };

        Assert.Equal(Id, run.Id);
        Assert.Equal("scheduled", run.Trigger);
        Assert.Equal(RunStatus.Incomplete, run.Status);
        Assert.Equal(Instant, run.StartedUtc);
        Assert.Equal(Instant.AddHours(1), run.FinishedUtc);
        Assert.Equal("The site refused five lookups in a row.", run.AbortReason);
    }



    [Fact]
    public void Run_when_it_has_just_been_created_is_in_progress_and_neither_finished_nor_aborted()
    {
        var run = new Run { Id = Id, Trigger = "manual" };

        Assert.Equal(RunStatus.InProgress, run.Status);
        Assert.Null(run.FinishedUtc);
        Assert.Null(run.AbortReason);
    }



    [Fact]
    public void Run_when_it_was_resumed_keeps_the_resume_and_the_work_done_before_it()
    {
        var run = new Run
        {
            Id = Id,
            Trigger = "scheduled",
            ResumedUtc = Instant.AddHours(3),
            WorkedSecondsBeforeResume = 1080,
        };

        Assert.Equal(Instant.AddHours(3), run.ResumedUtc);
        Assert.Equal(1080, run.WorkedSecondsBeforeResume);
    }



    [Fact]
    public void Run_when_it_was_never_resumed_has_no_resume_and_no_earlier_work()
    {
        var run = new Run { Id = Id, Trigger = "manual" };

        Assert.Null(run.ResumedUtc);
        Assert.Equal(0, run.WorkedSecondsBeforeResume);
    }



    [Fact]
    public void Run_when_a_worker_has_claimed_work_keeps_the_worker_and_the_last_claim()
    {
        var run = new Run
        {
            Id = Id,
            Trigger = "scheduled",
            WorkerName = "scraper-1",
            WorkerBuildVersion = "0.4.0",
            LastClaimUtc = Instant.AddMinutes(12),
        };

        Assert.Equal("scraper-1", run.WorkerName);
        Assert.Equal("0.4.0", run.WorkerBuildVersion);
        Assert.Equal(Instant.AddMinutes(12), run.LastClaimUtc);
    }



    [Fact]
    public void Run_when_nothing_has_been_claimed_names_no_worker()
    {
        var run = new Run { Id = Id, Trigger = "manual" };

        Assert.Null(run.WorkerName);
        Assert.Null(run.WorkerBuildVersion);
        Assert.Null(run.LastClaimUtc);
    }



    [Fact]
    public void MatrixRun_when_every_value_is_given_keeps_each_value()
    {
        var matrixRun = new MatrixRun
        {
            Id = Id,
            RunId = Parent,
            SearchMatrixId = Other,
            FirstCheckIn = new DateOnly(2027, 2, 1),
            LastCheckIn = new DateOnly(2027, 2, 21),
            Stage = SearchRunStage.Aggregated,
            Outcome = RunStatus.Complete,
        };

        Assert.Equal(Id, matrixRun.Id);
        Assert.Equal(Parent, matrixRun.RunId);
        Assert.Equal(Other, matrixRun.SearchMatrixId);
        Assert.Equal(new DateOnly(2027, 2, 1), matrixRun.FirstCheckIn);
        Assert.Equal(new DateOnly(2027, 2, 21), matrixRun.LastCheckIn);
        Assert.Equal(SearchRunStage.Aggregated, matrixRun.Stage);
        Assert.Equal(RunStatus.Complete, matrixRun.Outcome);
    }



    [Fact]
    public void MatrixRun_when_no_outcome_is_given_is_in_progress()
    {
        var matrixRun = new MatrixRun
        {
            Id = Id,
            RunId = Parent,
            SearchMatrixId = Other,
            FirstCheckIn = new DateOnly(2027, 2, 1),
            LastCheckIn = new DateOnly(2027, 2, 21),
            Stage = SearchRunStage.Exploded,
        };

        Assert.Equal(RunStatus.InProgress, matrixRun.Outcome);
        Assert.Null(matrixRun.SkipReason);
    }



    [Fact]
    public void MatrixRun_when_the_run_left_the_search_out_keeps_the_reason()
    {
        var matrixRun = new MatrixRun
        {
            Id = Id,
            RunId = Parent,
            SearchMatrixId = Other,
            FirstCheckIn = new DateOnly(2026, 10, 1),
            LastCheckIn = new DateOnly(2026, 10, 21),
            Stage = SearchRunStage.NotSet,
            SkipReason = "The window has passed.",
        };

        Assert.Equal("The window has passed.", matrixRun.SkipReason);
    }



    [Theory]
    [InlineData(WorkStatus.NotSet, 0)]
    [InlineData(WorkStatus.Pending, 1)]
    [InlineData(WorkStatus.Succeeded, 2)]
    [InlineData(WorkStatus.Failed, 3)]
    public void WorkStatus_when_cast_to_a_number_keeps_its_declared_value(WorkStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }



    [Fact]
    public void SearchRequest_when_it_has_been_claimed_and_answered_keeps_each_value()
    {
        var attempt = Guid.Parse("a1b2c3d4-e5f6-4789-8abc-def012345678");
        var request = new SearchRequest
        {
            Id = Id,
            RunId = Parent,
            ResortId = Other,
            CheckIn = new DateOnly(2027, 2, 13),
            CheckOut = new DateOnly(2027, 2, 20),
            AdultCount = 2,
            ChildCount = 1,
            InfantCount = 1,
            RunOrder = 75,
            Status = WorkStatus.Failed,
            AttemptCount = 2,
            LastError = "The page did not load.",
            CapturedUtc = Instant,
            LeasedBy = "scraper-1",
            LeaseExpiresUtc = Instant.AddMinutes(5),
            Attempt = attempt,
            ResultSha256 = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08",
            Version = 5,
        };

        Assert.Equal(Id, request.Id);
        Assert.Equal(Parent, request.RunId);
        Assert.Equal(Other, request.ResortId);
        Assert.Equal(new DateOnly(2027, 2, 13), request.CheckIn);
        Assert.Equal(new DateOnly(2027, 2, 20), request.CheckOut);
        Assert.Equal(2, request.AdultCount);
        Assert.Equal(1, request.ChildCount);
        Assert.Equal(1, request.InfantCount);
        Assert.Equal(75, request.RunOrder);
        Assert.Equal(WorkStatus.Failed, request.Status);
        Assert.Equal(2, request.AttemptCount);
        Assert.Equal("The page did not load.", request.LastError);
        Assert.Equal(Instant, request.CapturedUtc);
        Assert.Equal("scraper-1", request.LeasedBy);
        Assert.Equal(Instant.AddMinutes(5), request.LeaseExpiresUtc);
        Assert.Equal(attempt, request.Attempt);
        Assert.Equal("9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08", request.ResultSha256);
        Assert.Equal(5, request.Version);
    }



    [Fact]
    public void SearchRequest_when_it_has_just_been_planned_has_no_lease_no_attempt_and_no_result()
    {
        var request = new SearchRequest
        {
            Id = Id,
            RunId = Parent,
            ResortId = Other,
            CheckIn = new DateOnly(2027, 2, 13),
            CheckOut = new DateOnly(2027, 2, 20),
            AdultCount = 2,
            RunOrder = 50,
            Status = WorkStatus.Pending,
        };

        Assert.Equal(0, request.ChildCount);
        Assert.Equal(0, request.InfantCount);
        Assert.Equal(0, request.AttemptCount);
        Assert.Null(request.LastError);
        Assert.Null(request.CapturedUtc);
        Assert.Null(request.LeasedBy);
        Assert.Null(request.LeaseExpiresUtc);
        Assert.Null(request.Attempt);
        Assert.Null(request.ResultSha256);
        Assert.Equal(0, request.Version);
    }



    [Fact]
    public void SearchItem_when_the_search_wants_flights_keeps_each_value()
    {
        var item = new SearchItem { Id = Id, MatrixRunId = Parent, SearchRequestId = Other, Cabin = Cabin.Business };

        Assert.Equal(Id, item.Id);
        Assert.Equal(Parent, item.MatrixRunId);
        Assert.Equal(Other, item.SearchRequestId);
        Assert.Equal(Cabin.Business, item.Cabin);
    }



    [Fact]
    public void SearchItem_when_the_search_wants_no_flights_has_no_cabin()
    {
        var item = new SearchItem { Id = Id, MatrixRunId = Parent, SearchRequestId = Other };

        Assert.Null(item.Cabin);
    }
}
