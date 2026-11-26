namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

public sealed class MigrationTargetTests
{
    private const string Initial = "20260101000000_InitialCreate";

    private const string AddRuns = "20260201000000_AddRuns";

    private const string AddRunsIndex = "20260301000000_AddRunsIndex";

    private static readonly string[] Shipped = [AddRunsIndex, Initial, AddRuns];



    [Theory]
    [InlineData(Initial, Initial)]
    [InlineData("20260101000000_initialcreate", Initial)]
    [InlineData("InitialCreate", Initial)]
    [InlineData("initialcreate", Initial)]
    [InlineData("  InitialCreate  ", Initial)]
    [InlineData("AddRunsIndex", AddRunsIndex)]
    [InlineData("202601", Initial)]
    [InlineData("20260201000000_", AddRuns)]
    [InlineData("Init", Initial)]
    [InlineData("init", Initial)]
    [InlineData("AddRunsI", AddRunsIndex)]
    public void Resolve_when_given_an_id_a_name_or_a_prefix_one_migration_has_finds_that_migration(string text, string expected)
    {
        var target = MigrationTarget.Resolve(Shipped, text);

        Assert.Equal(MigrationTargetKind.Found, target.Kind);
        Assert.Equal(expected, target.Id);
        Assert.Empty(target.Candidates);
    }



    [Fact]
    public void Resolve_when_a_name_starts_another_name_prefers_the_whole_name()
    {
        var target = MigrationTarget.Resolve(Shipped, "AddRuns");

        Assert.Equal(MigrationTargetKind.Found, target.Kind);
        Assert.Equal(AddRuns, target.Id);
    }



    [Theory]
    [InlineData("2026")]
    [InlineData("Add")]
    [InlineData("addr")]
    public void Resolve_when_given_a_prefix_several_migrations_have_is_ambiguous_and_lists_them_oldest_first(string text)
    {
        var target = MigrationTarget.Resolve(Shipped, text);

        Assert.Equal(MigrationTargetKind.Ambiguous, target.Kind);
        Assert.Null(target.Id);
        Assert.Equal(Shipped.Order(StringComparer.Ordinal).Where(id => id.Contains(text, StringComparison.OrdinalIgnoreCase)), target.Candidates);
    }



    [Fact]
    public void Resolve_when_two_migrations_share_a_name_is_ambiguous()
    {
        const string again = "20260401000000_AddRuns";

        var target = MigrationTarget.Resolve([again, .. Shipped], "AddRuns");

        Assert.Equal(MigrationTargetKind.Ambiguous, target.Kind);
        Assert.Null(target.Id);
        Assert.Equal([AddRuns, again], target.Candidates);
    }



    [Theory]
    [InlineData("Nothing")]
    [InlineData("itialCreate")]
    [InlineData("20260101000000_InitialCreateX")]
    [InlineData("1")]
    [InlineData("00")]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_when_no_migration_matches_finds_nothing(string text)
    {
        var target = MigrationTarget.Resolve(Shipped, text);

        Assert.Equal(MigrationTargetKind.NotFound, target.Kind);
        Assert.Null(target.Id);
        Assert.Empty(target.Candidates);
    }



    [Theory]
    [InlineData("0")]
    [InlineData(" 0 ")]
    public void Resolve_when_given_zero_finds_the_empty_schema(string text)
    {
        var target = MigrationTarget.Resolve(Shipped, text);

        Assert.Equal(MigrationTargetKind.Found, target.Kind);
        Assert.Equal(MigrationTarget.EmptySchema, target.Id);
        Assert.Empty(target.Candidates);
    }



    [Fact]
    public void Resolve_when_given_zero_and_nothing_is_shipped_finds_the_empty_schema()
    {
        var target = MigrationTarget.Resolve([], "0");

        Assert.Equal(MigrationTargetKind.Found, target.Kind);
        Assert.Equal("0", target.Id);
    }



    [Fact]
    public void Resolve_when_nothing_is_shipped_finds_nothing()
    {
        var target = MigrationTarget.Resolve([], "InitialCreate");

        Assert.Equal(MigrationTargetKind.NotFound, target.Kind);
    }



    [Fact]
    public void Resolve_when_shipped_is_null_throws_ArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => MigrationTarget.Resolve(null!, "0"));

        Assert.Equal("shipped", exception.ParamName);
    }



    [Fact]
    public void Resolve_when_text_is_null_throws_ArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => MigrationTarget.Resolve(Shipped, null!));

        Assert.Equal("text", exception.ParamName);
    }



    [Theory]
    [InlineData("20260101000000_InitialCreate", "InitialCreate")]
    [InlineData("20260101000000_Add_Runs", "Add_Runs")]
    [InlineData("NoUnderscore", "NoUnderscore")]
    [InlineData("Trailing_", "")]
    public void NameOf_when_given_an_id_gives_the_part_after_the_first_underscore(string id, string expected)
    {
        Assert.Equal(expected, MigrationTarget.NameOf(id));
    }



    [Fact]
    public void NameOf_when_id_is_null_throws_ArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => MigrationTarget.NameOf(null!));

        Assert.Equal("id", exception.ParamName);
    }



    [Fact]
    public void MigrationTargetKind_when_read_as_numbers_keeps_its_values()
    {
        Assert.Equal(0, (int)MigrationTargetKind.Found);
        Assert.Equal(1, (int)MigrationTargetKind.NotFound);
        Assert.Equal(2, (int)MigrationTargetKind.Ambiguous);
    }
}
