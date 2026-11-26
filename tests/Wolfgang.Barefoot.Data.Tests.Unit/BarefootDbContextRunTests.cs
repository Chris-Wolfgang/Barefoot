using Microsoft.EntityFrameworkCore;
using Wolfgang.Barefoot.Contracts.Runs;
using Wolfgang.Barefoot.Contracts.Searches;
using Wolfgang.Barefoot.Core.Entities;

namespace Wolfgang.Barefoot.Data.Tests.Unit;

/// <summary>
/// The run tables of <see cref="BarefootDbContext"/>: a run, each search's part in it, its room
/// lookups and the links from searches to lookups.
/// </summary>
public sealed class BarefootDbContextRunTests
{
    private static readonly Guid OwnerOid = Guid.Parse("d1e8f90a-1223-43c4-8e56-f708192a3b4c");

    private static readonly Guid SearchId = Guid.Parse("e2f90a1b-2334-44d5-9f67-08192a3b4c5d");

    private static readonly Guid OtherSearchId = Guid.Parse("f30a1b2c-3445-45e6-a078-192a3b4c5d6e");

    private static readonly Guid ResortId = Guid.Parse("041b2c3d-4556-46f7-b189-2a3b4c5d6e7f");

    private static readonly Guid RunId = Guid.Parse("152c3d4e-5667-4708-8c9a-3b4c5d6e7f80");

    private static readonly Guid OtherRunId = Guid.Parse("263d4e5f-6778-4819-9dab-4c5d6e7f8091");

    private static readonly Guid MatrixRunId = Guid.Parse("374e5f60-7889-492a-aebc-5d6e7f8091a2");

    private static readonly Guid OtherMatrixRunId = Guid.Parse("485f6071-899a-4a3b-bfcd-6e7f8091a2b3");

    private static readonly Guid RequestId = Guid.Parse("59607182-9aab-4b4c-80de-7f8091a2b3c4");

    private static readonly Guid OtherRequestId = Guid.Parse("6a718293-abbc-4c5d-91ef-8091a2b3c4d5");

    private static readonly Guid RowId = Guid.Parse("7b8293a4-bccd-4d6e-a2f0-91a2b3c4d5e6");

    private static readonly Guid OtherRowId = Guid.Parse("8c93a4b5-cdde-4e7f-b301-a2b3c4d5e6f7");

    private static readonly Guid AttemptToken = Guid.Parse("9da4b5c6-deef-4f80-8412-b3c4d5e6f708");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 40, 0, 123, TimeSpan.Zero);

    private static readonly DateOnly CheckIn = new(2027, 1, 9);



    [Fact]
    public async Task SaveChangesAsync_when_a_run_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Runs.Add
            (
                new Run
                {
                    Id = RunId,
                    Trigger = "schedule",
                    Status = RunStatus.Incomplete,
                    StartedUtc = Instant,
                    FinishedUtc = Instant.AddHours(3),
                    AbortReason = "The site refused the worker.",
                    ResumedUtc = Instant.AddHours(1),
                    WorkedSecondsBeforeResume = 1800,
                    WorkerName = "worker-1",
                    WorkerBuildVersion = "2.1.0+3f9c2ab",
                    LastClaimUtc = Instant.AddHours(2),
                }
            );
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.Runs.SingleAsync();

        Assert.Equal(RunId, read.Id);
        Assert.Equal("schedule", read.Trigger);
        Assert.Equal(RunStatus.Incomplete, read.Status);
        Assert.Equal(Instant, read.StartedUtc);
        Assert.Equal(Instant.AddHours(3), read.FinishedUtc);
        Assert.Equal("The site refused the worker.", read.AbortReason);
        Assert.Equal(Instant.AddHours(1), read.ResumedUtc);
        Assert.Equal(1800, read.WorkedSecondsBeforeResume);
        Assert.Equal("worker-1", read.WorkerName);
        Assert.Equal("2.1.0+3f9c2ab", read.WorkerBuildVersion);
        Assert.Equal(Instant.AddHours(2), read.LastClaimUtc);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_run_has_just_been_created_reads_back_a_run_in_progress_with_nothing_else_set()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Runs.Add(Run(RunId, RunStatus.InProgress));
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.Runs.SingleAsync();

        Assert.Equal(RunStatus.InProgress, read.Status);
        Assert.Null(read.FinishedUtc);
        Assert.Null(read.AbortReason);
        Assert.Null(read.ResumedUtc);
        Assert.Equal(0, read.WorkedSecondsBeforeResume);
        Assert.Null(read.WorkerName);
        Assert.Null(read.WorkerBuildVersion);
        Assert.Null(read.LastClaimUtc);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_second_run_is_in_progress_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Runs.Add(Run(RunId, RunStatus.InProgress));
            await context.SaveChangesAsync();
        }

        await using var second = database.NewContext();
        second.Runs.Add(Run(OtherRunId, RunStatus.InProgress));

        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }



    [Theory]
    [InlineData(RunStatus.Complete)]
    [InlineData(RunStatus.Incomplete)]
    [InlineData(RunStatus.Failed)]
    public async Task SaveChangesAsync_when_two_runs_ended_the_same_way_saves_both(RunStatus status)
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.Runs.Add(Run(RunId, status));
        context.Runs.Add(Run(OtherRunId, status));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Runs.CountAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_the_run_in_progress_is_finished_lets_the_next_run_start()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            context.Runs.Add(Run(RunId, RunStatus.InProgress));
            await context.SaveChangesAsync();
        }

        await using var next = database.NewContext();
        var first = await next.Runs.SingleAsync();
        first.Status = RunStatus.Complete;
        next.Runs.Add(Run(OtherRunId, RunStatus.InProgress));
        await next.SaveChangesAsync();

        Assert.Equal(OtherRunId, (await next.Runs.SingleAsync(run => run.Status == RunStatus.InProgress)).Id);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_matrix_run_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddSearchResortAndRun(context);
            var matrixRun = MatrixRun(MatrixRunId, RunId, SearchId);
            matrixRun.Stage = SearchRunStage.Downloaded;
            matrixRun.Outcome = RunStatus.Complete;
            matrixRun.SkipReason = "Its window had passed.";
            context.MatrixRuns.Add(matrixRun);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.MatrixRuns.SingleAsync();

        Assert.Equal(MatrixRunId, read.Id);
        Assert.Equal(RunId, read.RunId);
        Assert.Equal(SearchId, read.SearchMatrixId);
        Assert.Equal(CheckIn, read.FirstCheckIn);
        Assert.Equal(CheckIn.AddDays(30), read.LastCheckIn);
        Assert.Equal(SearchRunStage.Downloaded, read.Stage);
        Assert.Equal(RunStatus.Complete, read.Outcome);
        Assert.Equal("Its window had passed.", read.SkipReason);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_request_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddSearchResortAndRun(context);
            var request = Request(RequestId, RunId, CheckIn);
            request.ChildCount = 1;
            request.InfantCount = 1;
            request.Status = WorkStatus.Failed;
            request.AttemptCount = 3;
            request.LastError = "The site answered 503.";
            request.CapturedUtc = Instant.AddMinutes(5);
            request.LeasedBy = "worker-1";
            request.LeaseExpiresUtc = Instant.AddMinutes(10);
            request.Attempt = AttemptToken;
            request.ResultSha256 = new string('a', 64);
            context.SearchRequests.Add(request);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.SearchRequests.SingleAsync();

        Assert.Equal(RequestId, read.Id);
        Assert.Equal(RunId, read.RunId);
        Assert.Equal(ResortId, read.ResortId);
        Assert.Equal(CheckIn, read.CheckIn);
        Assert.Equal(CheckIn.AddDays(7), read.CheckOut);
        Assert.Equal(2, read.AdultCount);
        Assert.Equal(1, read.ChildCount);
        Assert.Equal(1, read.InfantCount);
        Assert.Equal(5, read.RunOrder);
        Assert.Equal(WorkStatus.Failed, read.Status);
        Assert.Equal(3, read.AttemptCount);
        Assert.Equal("The site answered 503.", read.LastError);
        Assert.Equal(Instant.AddMinutes(5), read.CapturedUtc);
        Assert.Equal("worker-1", read.LeasedBy);
        Assert.Equal(Instant.AddMinutes(10), read.LeaseExpiresUtc);
        Assert.Equal(AttemptToken, read.Attempt);
        Assert.Equal(new string('a', 64), read.ResultSha256);
        Assert.Equal(1, read.Version);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_request_has_not_been_claimed_reads_back_no_lease_and_no_result()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddSearchResortAndRun(context);
            context.SearchRequests.Add(Request(RequestId, RunId, CheckIn));
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.SearchRequests.SingleAsync();

        Assert.Equal(WorkStatus.Pending, read.Status);
        Assert.Equal(0, read.AttemptCount);
        Assert.Null(read.LastError);
        Assert.Null(read.CapturedUtc);
        Assert.Null(read.LeasedBy);
        Assert.Null(read.LeaseExpiresUtc);
        Assert.Null(read.Attempt);
        Assert.Null(read.ResultSha256);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_item_is_added_reads_back_every_value()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddSearchResortAndRun(context);
            context.MatrixRuns.Add(MatrixRun(MatrixRunId, RunId, SearchId));
            context.SearchRequests.Add(Request(RequestId, RunId, CheckIn));
            context.SearchItems.Add
            (
                new SearchItem { Id = RowId, MatrixRunId = MatrixRunId, SearchRequestId = RequestId, Cabin = Cabin.Business }
            );
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var read = await reader.SearchItems.SingleAsync();

        Assert.Equal(RowId, read.Id);
        Assert.Equal(MatrixRunId, read.MatrixRunId);
        Assert.Equal(RequestId, read.SearchRequestId);
        Assert.Equal(Cabin.Business, read.Cabin);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_takes_part_in_a_run_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddSearchResortAndRun(context);
        context.MatrixRuns.Add(MatrixRun(MatrixRunId, RunId, SearchId));
        context.MatrixRuns.Add(MatrixRun(OtherMatrixRunId, RunId, SearchId));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_takes_part_in_two_runs_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddSearchResortAndRun(context);
        context.Runs.Add(Run(OtherRunId, RunStatus.Complete));
        context.MatrixRuns.Add(MatrixRun(MatrixRunId, RunId, SearchId));
        context.MatrixRuns.Add(MatrixRun(OtherMatrixRunId, OtherRunId, SearchId));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.MatrixRuns.CountAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_run_has_the_same_lookup_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddSearchResortAndRun(context);
        context.SearchRequests.Add(Request(RequestId, RunId, CheckIn));
        var again = Request(OtherRequestId, RunId, CheckIn);
        again.RunOrder = 9;
        context.SearchRequests.Add(again);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Theory]
    [InlineData("check-in")]
    [InlineData("check-out")]
    [InlineData("adults")]
    [InlineData("children")]
    [InlineData("infants")]
    public async Task SaveChangesAsync_when_two_lookups_of_a_run_differ_in_one_part_of_the_stay_saves_both(string part)
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddSearchResortAndRun(context);
        context.SearchRequests.Add(Request(RequestId, RunId, CheckIn));
        var other = Request(OtherRequestId, RunId, CheckIn);

        switch (part)
        {
            case "check-in":
                other.CheckIn = CheckIn.AddDays(-1);
                break;
            case "check-out":
                other.CheckOut = CheckIn.AddDays(8);
                break;
            case "adults":
                other.AdultCount = 3;
                break;
            case "children":
                other.ChildCount = 1;
                break;
            default:
                other.InfantCount = 1;
                break;
        }

        context.SearchRequests.Add(other);

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.SearchRequests.CountAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_runs_have_the_same_lookup_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddSearchResortAndRun(context);
        context.Runs.Add(Run(OtherRunId, RunStatus.Complete));
        context.SearchRequests.Add(Request(RequestId, RunId, CheckIn));
        context.SearchRequests.Add(Request(OtherRequestId, OtherRunId, CheckIn));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.SearchRequests.CountAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_links_to_a_lookup_twice_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddSearchResortAndRun(context);
        context.MatrixRuns.Add(MatrixRun(MatrixRunId, RunId, SearchId));
        context.SearchRequests.Add(Request(RequestId, RunId, CheckIn));
        context.SearchItems.Add(new SearchItem { Id = RowId, MatrixRunId = MatrixRunId, SearchRequestId = RequestId });
        context.SearchItems.Add(new SearchItem { Id = OtherRowId, MatrixRunId = MatrixRunId, SearchRequestId = RequestId });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_two_searches_link_to_the_same_lookup_saves_both()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddSearchResortAndRun(context);
        context.SearchMatrices.Add(Search(OtherSearchId, "Spring break"));
        context.MatrixRuns.Add(MatrixRun(MatrixRunId, RunId, SearchId));
        context.MatrixRuns.Add(MatrixRun(OtherMatrixRunId, RunId, OtherSearchId));
        context.SearchRequests.Add(Request(RequestId, RunId, CheckIn));
        context.SearchItems.Add(new SearchItem { Id = RowId, MatrixRunId = MatrixRunId, SearchRequestId = RequestId });
        context.SearchItems.Add(new SearchItem { Id = OtherRowId, MatrixRunId = OtherMatrixRunId, SearchRequestId = RequestId });

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.SearchItems.CountAsync());
        Assert.Null((await context.SearchItems.FirstAsync()).Cabin);
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_matrix_run_names_a_run_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddSearchResortAndRun(context);
        context.MatrixRuns.Add(MatrixRun(MatrixRunId, OtherRunId, SearchId));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_request_names_a_resort_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        context.Runs.Add(Run(RunId, RunStatus.InProgress));
        context.SearchRequests.Add(Request(RequestId, RunId, CheckIn));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_search_item_names_a_lookup_that_does_not_exist_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();
        AddSearchResortAndRun(context);
        context.MatrixRuns.Add(MatrixRun(MatrixRunId, RunId, SearchId));
        context.SearchItems.Add(new SearchItem { Id = RowId, MatrixRunId = MatrixRunId, SearchRequestId = RequestId });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_a_run_that_has_lookups_is_deleted_throws_DbUpdateException()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddSearchResortAndRun(context);
            context.SearchRequests.Add(Request(RequestId, RunId, CheckIn));
            await context.SaveChangesAsync();
        }

        await using var deleter = database.NewContext();
        deleter.Runs.Remove(await deleter.Runs.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => deleter.SaveChangesAsync());
    }



    [Fact]
    public async Task ToListAsync_when_filtered_by_lease_expiry_finds_the_lookups_whose_lease_has_run_out_in_the_database()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using (var context = database.NewContext())
        {
            AddSearchResortAndRun(context);
            var expired = Request(RequestId, RunId, CheckIn);
            expired.LeaseExpiresUtc = Instant.AddMilliseconds(-1);
            var held = Request(OtherRequestId, RunId, CheckIn.AddDays(1));
            held.LeaseExpiresUtc = Instant.AddMilliseconds(1);
            var free = Request(RowId, RunId, CheckIn.AddDays(2));
            context.SearchRequests.AddRange(expired, held, free);
            await context.SaveChangesAsync();
        }

        await using var reader = database.NewContext();
        var claimable = await reader
            .SearchRequests
            .Where(request => request.LeaseExpiresUtc == null || request.LeaseExpiresUtc < Instant)
            .OrderBy(request => request.CheckIn)
            .Select(request => request.Id)
            .ToListAsync();

        Assert.Equal([RequestId, RowId], claimable);
    }



    private static void AddSearchResortAndRun(BarefootDbContext context)
    {
        context.AppUsers.Add(new AppUser { Oid = OwnerOid, DisplayName = "Pat Example", Email = "pat@example.com", LastSeenUtc = Instant });
        context.SearchMatrices.Add(Search(SearchId, "Winter week"));
        context.Resorts.Add
        (
            new Resort
            {
                Id = ResortId,
                Code = "RES1",
                Name = "Example Bay Resort",
                Brand = Brand.Sandals,
                Island = "Example Island",
                FirstSeenUtc = Instant,
            }
        );
        context.Runs.Add(Run(RunId, RunStatus.InProgress));
    }



    private static SearchMatrix Search(Guid id, string name)
    {
        return new SearchMatrix
        {
            Id = id,
            OwnerId = OwnerOid,
            Name = name,
            NormalizedName = SearchMatrix.NormalizeName(name),
            Brand = Brand.Sandals,
            NightCount = 7,
            AdultCount = 2,
            WindowKind = WindowKind.Recurring,
            CreatedUtc = Instant,
        };
    }



    private static Run Run(Guid id, RunStatus status)
    {
        return new Run { Id = id, Trigger = "schedule", Status = status, StartedUtc = Instant };
    }



    private static MatrixRun MatrixRun(Guid id, Guid runId, Guid searchId)
    {
        return new MatrixRun
        {
            Id = id,
            RunId = runId,
            SearchMatrixId = searchId,
            FirstCheckIn = CheckIn,
            LastCheckIn = CheckIn.AddDays(30),
            Stage = SearchRunStage.Exploded,
        };
    }



    private static SearchRequest Request(Guid id, Guid runId, DateOnly checkIn)
    {
        return new SearchRequest
        {
            Id = id,
            RunId = runId,
            ResortId = ResortId,
            CheckIn = checkIn,
            CheckOut = checkIn.AddDays(7),
            AdultCount = 2,
            RunOrder = 5,
            Status = WorkStatus.Pending,
        };
    }
}
