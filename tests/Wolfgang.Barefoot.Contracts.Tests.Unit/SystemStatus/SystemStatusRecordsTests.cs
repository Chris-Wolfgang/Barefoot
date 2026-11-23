using Wolfgang.Barefoot.Contracts.Runs;
using Wolfgang.Barefoot.Contracts.SystemStatus;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.SystemStatus;

public sealed class SystemStatusRecordsTests
{
    private static readonly Guid RunId = Guid.Parse("5d0c6f0e-3a51-4c1d-8a43-1b2c3d4e5f60");

    private static readonly DateTimeOffset Instant = new(2026, 11, 4, 6, 41, 0, TimeSpan.Zero);



    [Theory]
    [InlineData(DatabaseStatus.NotSet, 0)]
    [InlineData(DatabaseStatus.Online, 1)]
    [InlineData(DatabaseStatus.Paused, 2)]
    public void DatabaseStatus_when_cast_to_a_number_keeps_its_declared_value(DatabaseStatus status, int expected)
    {
        Assert.Equal(expected, (int)status);
    }



    [Theory]
    [InlineData(AttentionTarget.NotSet, 0)]
    [InlineData(AttentionTarget.Deliveries, 1)]
    [InlineData(AttentionTarget.ReferenceData, 2)]
    [InlineData(AttentionTarget.Users, 3)]
    [InlineData(AttentionTarget.Runs, 4)]
    public void AttentionTarget_when_cast_to_a_number_keeps_its_declared_value(AttentionTarget target, int expected)
    {
        Assert.Equal(expected, (int)target);
    }



    [Fact]
    public void BudgetResponse_when_the_platform_reports_every_figure_keeps_each_value()
    {
        var budget = Budget();

        Assert.Equal(DatabaseStatus.Paused, budget.DatabaseStatus);
        Assert.Equal(31, budget.DatabaseAllowanceUsedPercent);
        Assert.Equal(new DateOnly(2026, 12, 1), budget.DatabaseAllowanceResetsOn);
        Assert.Equal(2_040_109_465, budget.StorageUsedBytes);
        Assert.Equal(34_359_738_368, budget.StorageLimitBytes);
        Assert.Equal(22, budget.ComputeUsedPercent);
        Assert.Equal(Instant.AddDays(-1), budget.ComputeAsOfUtc);
    }



    [Fact]
    public void BudgetResponse_when_the_platform_cannot_be_read_has_no_figures()
    {
        var budget = new BudgetResponse(DatabaseStatus.NotSet, null, null, null, null, null, null);

        Assert.Equal(DatabaseStatus.NotSet, budget.DatabaseStatus);
        Assert.Null(budget.DatabaseAllowanceUsedPercent);
        Assert.Null(budget.DatabaseAllowanceResetsOn);
        Assert.Null(budget.StorageUsedBytes);
        Assert.Null(budget.StorageLimitBytes);
        Assert.Null(budget.ComputeUsedPercent);
        Assert.Null(budget.ComputeAsOfUtc);
    }



    [Fact]
    public void AttentionItemResponse_when_every_value_is_given_keeps_each_value()
    {
        var item = new AttentionItemResponse("2 deliveries failed", "The mail server refused them.", AttentionTarget.Deliveries);

        Assert.Equal("2 deliveries failed", item.Title);
        Assert.Equal("The mail server refused them.", item.Detail);
        Assert.Equal(AttentionTarget.Deliveries, item.Target);
    }



    [Fact]
    public void SystemDashboardResponse_when_there_has_been_a_run_keeps_each_value()
    {
        var lastRun = new RunHistoryResponse
        (
            RunId,
            "scheduled",
            Instant.AddMinutes(-41),
            null,
            RunStatus.Complete,
            41,
            new WorkProgress(212, 212, 2),
            new WorkProgress(38, 38, 0),
            44,
            2,
            11.6m,
            null
        );
        var budget = Budget();
        var item = new AttentionItemResponse("2 deliveries failed", "The mail server refused them.", AttentionTarget.Deliveries);
        var dashboard = new SystemDashboardResponse(lastRun, budget, 44, 2, "scraper-1", Instant, 9, 7, 22, [item]);

        Assert.Equal(lastRun, dashboard.LastRun);
        Assert.Equal(budget, dashboard.Budget);
        Assert.Equal(44, dashboard.MonthSentDigestCount);
        Assert.Equal(2, dashboard.MonthFailedDigestCount);
        Assert.Equal("scraper-1", dashboard.WorkerName);
        Assert.Equal(Instant, dashboard.WorkerLastSeenUtc);
        Assert.Equal(9, dashboard.UserCount);
        Assert.Equal(7, dashboard.ActiveUserCount);
        Assert.Equal(22, dashboard.ActiveSearchCount);
        Assert.Equal(item, Assert.Single(dashboard.NeedsAttention));
    }



    [Fact]
    public void SystemDashboardResponse_when_nothing_has_run_yet_has_no_last_run_and_no_worker()
    {
        var dashboard = new SystemDashboardResponse(null, Budget(), 0, 0, null, null, 1, 0, 0, []);

        Assert.Null(dashboard.LastRun);
        Assert.Null(dashboard.WorkerName);
        Assert.Null(dashboard.WorkerLastSeenUtc);
        Assert.Empty(dashboard.NeedsAttention);
    }



    [Fact]
    public void RunningResponse_when_a_worker_has_been_seen_keeps_each_value()
    {
        var running = new RunningResponse("0.4.0", "v0", 7, "20261201_InitialCreate", true, "scraper-1", "0.4.0", Instant);

        Assert.Equal("0.4.0", running.ApiBuildVersion);
        Assert.Equal("v0", running.ContractVersion);
        Assert.Equal(7, running.Revision);
        Assert.Equal("20261201_InitialCreate", running.SchemaMigration);
        Assert.True(running.SchemaUpToDate);
        Assert.Equal("scraper-1", running.WorkerName);
        Assert.Equal("0.4.0", running.WorkerBuildVersion);
        Assert.Equal(Instant, running.WorkerLastSeenUtc);
    }



    [Fact]
    public void RunningResponse_when_the_schema_cannot_be_read_and_no_worker_was_seen_names_neither()
    {
        var running = new RunningResponse("0.4.0", "v0", 7, null, false, null, null, null);

        Assert.Null(running.SchemaMigration);
        Assert.False(running.SchemaUpToDate);
        Assert.Null(running.WorkerName);
        Assert.Null(running.WorkerBuildVersion);
        Assert.Null(running.WorkerLastSeenUtc);
    }



    private static BudgetResponse Budget()
    {
        return new BudgetResponse
        (
            DatabaseStatus.Paused,
            31,
            new DateOnly(2026, 12, 1),
            2_040_109_465,
            34_359_738_368,
            22,
            Instant.AddDays(-1)
        );
    }
}
