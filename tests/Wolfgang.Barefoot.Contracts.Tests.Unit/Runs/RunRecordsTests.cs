using Wolfgang.Barefoot.Contracts.Runs;
using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Runs;

public sealed class RunRecordsTests
{
    private static readonly Guid Id = Guid.Parse("11111111-2222-4333-8444-555555555555");

    private static readonly Guid Attempt = Guid.Parse("7b0e3c52-1f4a-4d6b-9a57-0c2d8e4f6a11");

    private static readonly DateTimeOffset Expires = new(2026, 11, 21, 9, 5, 0, TimeSpan.Zero);



    [Fact]
    public void RunResponse_when_built_keeps_the_run_its_searches_and_the_worker_values()
    {
        var search = Guid.Parse("3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b0c9");
        var run = new RunResponse(Id, RunStatus.InProgress, [search], new WorkerSettings(2500, 800));

        Assert.Equal(Id, run.Id);
        Assert.Equal(RunStatus.InProgress, run.Status);
        Assert.Equal([search], run.SearchIds);
        Assert.Equal(2500, run.Worker.PolitenessDelayMs);
        Assert.Equal(800, run.Worker.PolitenessJitterMs);
    }



    [Fact]
    public void ClaimedLookup_when_built_keeps_each_value()
    {
        var claimed = new ClaimedLookup(Id, Attempt, Expires, "SGL", new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 8), 2, 1, 0);

        Assert.Equal(Id, claimed.RequestId);
        Assert.Equal(Attempt, claimed.Attempt);
        Assert.Equal(Expires, claimed.LeaseExpiresUtc);
        Assert.Equal("SGL", claimed.ResortCode);
        Assert.Equal(new DateOnly(2027, 3, 1), claimed.CheckIn);
        Assert.Equal(new DateOnly(2027, 3, 8), claimed.CheckOut);
        Assert.Equal(2, claimed.Adults);
        Assert.Equal(1, claimed.Children);
        Assert.Equal(0, claimed.Infants);
    }



    [Fact]
    public void ClaimedFlightSet_when_built_keeps_each_value()
    {
        var claimed = new ClaimedFlightSet
        (
            Id,
            Attempt,
            Expires,
            "SGL",
            "AAA",
            "UVF",
            new DateOnly(2027, 3, 1),
            new DateOnly(2027, 3, 8),
            Cabin.Business,
            2,
            1,
            1
        );

        Assert.Equal(Id, claimed.FlightSetId);
        Assert.Equal(Attempt, claimed.Attempt);
        Assert.Equal(Expires, claimed.LeaseExpiresUtc);
        Assert.Equal("SGL", claimed.ResortCode);
        Assert.Equal("AAA", claimed.DepartureAirportCode);
        Assert.Equal("UVF", claimed.GatewayAirportCode);
        Assert.Equal(new DateOnly(2027, 3, 1), claimed.DepartDate);
        Assert.Equal(new DateOnly(2027, 3, 8), claimed.ReturnDate);
        Assert.Equal(Cabin.Business, claimed.Cabin);
        Assert.Equal(2, claimed.Adults);
        Assert.Equal(1, claimed.Children);
        Assert.Equal(1, claimed.Infants);
    }



    [Fact]
    public void Requests_when_built_keep_their_one_value()
    {
        Assert.Equal("scheduled", new StartRunRequest("scheduled").Trigger);
        Assert.Equal("home-1", new ClaimRequest("home-1").Worker);
        Assert.Equal("Three lookups in a row failed.", new AbortRunRequest("Three lookups in a row failed.").Reason);
    }



    [Fact]
    public void SearchRunResponse_when_built_keeps_the_search_and_its_outcome()
    {
        var response = new SearchRunResponse(Id, RunStatus.Incomplete);

        Assert.Equal(Id, response.SearchId);
        Assert.Equal(RunStatus.Incomplete, response.Outcome);
    }



    [Theory]
    [InlineData(RunStatus.InProgress, 0)]
    [InlineData(RunStatus.Complete, 1)]
    [InlineData(RunStatus.Incomplete, 2)]
    [InlineData(RunStatus.Failed, 3)]
    public void RunStatus_when_cast_to_a_number_keeps_its_declared_value(RunStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }
}
