namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

public sealed class MigrationPlanTests
{
    private const string A = "20260101000000_A";

    private const string B = "20260201000000_B";

    private const string C = "20260301000000_C";

    private static readonly string[] Shipped = [C, A, B];



    [Fact]
    public void Make_when_nothing_is_applied_and_the_target_is_the_latest_applies_everything_oldest_first()
    {
        var plan = MigrationPlan.Make(Shipped, [], C);

        Assert.Equal([A, B, C], plan.Apply);
        Assert.Empty(plan.Revert);
        Assert.Equal("up", plan.Direction);
    }



    [Fact]
    public void Make_when_some_are_applied_applies_only_the_rest_up_to_the_target()
    {
        var plan = MigrationPlan.Make(Shipped, [A], B);

        Assert.Equal([B], plan.Apply);
        Assert.Empty(plan.Revert);
        Assert.Equal("up", plan.Direction);
    }



    [Fact]
    public void Make_when_the_target_is_behind_the_database_rolls_back_newest_first()
    {
        var plan = MigrationPlan.Make(Shipped, [A, B, C], A);

        Assert.Empty(plan.Apply);
        Assert.Equal([C, B], plan.Revert);
        Assert.Equal("down", plan.Direction);
    }



    [Fact]
    public void Make_when_the_target_is_the_empty_schema_rolls_back_everything_applied()
    {
        var plan = MigrationPlan.Make(Shipped, [A, B], "0");

        Assert.Empty(plan.Apply);
        Assert.Equal([B, A], plan.Revert);
        Assert.Equal("down", plan.Direction);
    }



    [Fact]
    public void Make_when_the_database_is_at_the_target_changes_nothing()
    {
        var plan = MigrationPlan.Make(Shipped, [A, B], B);

        Assert.Empty(plan.Apply);
        Assert.Empty(plan.Revert);
        Assert.Equal("none", plan.Direction);
    }



    [Fact]
    public void Make_when_nothing_is_applied_and_the_target_is_the_empty_schema_changes_nothing()
    {
        var plan = MigrationPlan.Make(Shipped, [], "0");

        Assert.Empty(plan.Apply);
        Assert.Empty(plan.Revert);
        Assert.Equal("none", plan.Direction);
    }



    [Fact]
    public void Make_when_an_older_migration_is_missing_and_a_newer_one_is_applied_does_both_and_calls_it_down()
    {
        var plan = MigrationPlan.Make(Shipped, [C], B);

        Assert.Equal([A, B], plan.Apply);
        Assert.Equal([C], plan.Revert);
        Assert.Equal("down", plan.Direction);
    }



    [Fact]
    public void Gaps_when_an_older_migration_is_missing_and_a_newer_one_is_applied_names_the_missing_ones()
    {
        Assert.Equal([A, B], MigrationPlan.Gaps(Shipped, [C]));
        Assert.Equal([B], MigrationPlan.Gaps(Shipped, [A, C]));
    }



    [Theory]
    [InlineData]
    [InlineData(A)]
    [InlineData(A, B)]
    [InlineData(A, B, C)]
    public void Gaps_when_the_applied_migrations_are_the_oldest_ones_finds_none(params string[] applied)
    {
        Assert.Empty(MigrationPlan.Gaps(Shipped, applied));
    }



    [Fact]
    public void Gaps_when_ids_differ_in_case_or_are_not_shipped_ignores_the_difference()
    {
        Assert.Empty(MigrationPlan.Gaps(Shipped, [A.ToUpperInvariant(), "20991231000000_Later"]));
        Assert.Equal([A], MigrationPlan.Gaps(Shipped, [B.ToUpperInvariant()]));
    }



    [Fact]
    public void Gaps_when_an_argument_is_null_throws_ArgumentNullException()
    {
        Assert.Equal("shipped", Assert.Throws<ArgumentNullException>(() => MigrationPlan.Gaps(null!, [])).ParamName);
        Assert.Equal("applied", Assert.Throws<ArgumentNullException>(() => MigrationPlan.Gaps(Shipped, null!)).ParamName);
    }



    [Fact]
    public void Make_when_ids_differ_in_case_treats_them_as_the_same()
    {
        var plan = MigrationPlan.Make(Shipped, [A.ToUpperInvariant(), B.ToLowerInvariant()], A.ToLowerInvariant());

        Assert.Empty(plan.Apply);
        Assert.Equal([B], plan.Revert);
    }



    [Fact]
    public void Make_when_the_database_holds_an_id_that_is_not_shipped_ignores_it()
    {
        var plan = MigrationPlan.Make(Shipped, [A, "99990101000000_Later"], A);

        Assert.Empty(plan.Apply);
        Assert.Empty(plan.Revert);
    }



    [Theory]
    [InlineData("20260401000000_D")]
    [InlineData("")]
    [InlineData("00")]
    public void Make_when_the_target_is_not_shipped_throws_ArgumentException(string targetId)
    {
        var exception = Assert.Throws<ArgumentException>(() => MigrationPlan.Make(Shipped, [], targetId));

        Assert.Equal("targetId", exception.ParamName);
        Assert.StartsWith($"This build ships no migration '{targetId}'.", exception.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void Make_when_shipped_is_null_throws_ArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => MigrationPlan.Make(null!, [], "0"));

        Assert.Equal("shipped", exception.ParamName);
    }



    [Fact]
    public void Make_when_applied_is_null_throws_ArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => MigrationPlan.Make(Shipped, null!, "0"));

        Assert.Equal("applied", exception.ParamName);
    }



    [Fact]
    public void Make_when_targetId_is_null_throws_ArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => MigrationPlan.Make(Shipped, [], null!));

        Assert.Equal("targetId", exception.ParamName);
    }
}
