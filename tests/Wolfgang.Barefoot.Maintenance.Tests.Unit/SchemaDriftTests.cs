namespace Wolfgang.Barefoot.Maintenance.Tests.Unit;

/// <summary>
/// The comparison behind <c>bf-maint verify-schema</c> (ADR 0016).
/// </summary>
public sealed class SchemaDriftTests
{
    [Fact]
    public void Compare_when_both_sides_have_the_same_lines_reports_nothing()
    {
        string[] expected = ["table run|", "column run.id|int not null"];
        string[] actual = ["column run.id|int not null", "table run|"];

        Assert.Empty(SchemaDrift.Compare(expected, actual));
    }



    [Fact]
    public void Compare_when_the_database_lacks_an_item_reports_it_missing()
    {
        string[] expected = ["table run|", "column run.id|int not null"];
        string[] actual = ["table run|"];

        Assert.Equal(["missing: column run.id"], SchemaDrift.Compare(expected, actual));
    }



    [Fact]
    public void Compare_when_the_database_has_an_item_the_model_lacks_reports_it_extra()
    {
        string[] expected = ["table run|"];
        string[] actual = ["table run|", "index run.ix_stray|not unique (id)"];

        Assert.Equal(["extra: index run.ix_stray"], SchemaDrift.Compare(expected, actual));
    }



    [Fact]
    public void Compare_when_an_item_has_another_detail_reports_both_details()
    {
        string[] expected = ["column run.id|int not null"];
        string[] actual = ["column run.id|bigint null"];

        Assert.Equal
        (
            ["different: column run.id is bigint null, expected int not null"],
            SchemaDrift.Compare(expected, actual)
        );
    }



    [Fact]
    public void Compare_when_details_differ_only_in_case_reports_the_difference()
    {
        string[] expected = ["column run.id|int not null"];
        string[] actual = ["column run.id|INT not null"];

        Assert.Single(SchemaDrift.Compare(expected, actual));
    }



    [Fact]
    public void Compare_when_there_are_several_differences_sorts_them_by_item()
    {
        string[] expected = ["table b|", "column a.x|int null", "table a|"];
        string[] actual = ["table c|", "column a.x|int not null", "table a|"];

        Assert.Equal
        (
            [
                "different: column a.x is int not null, expected int null",
                "missing: table b",
                "extra: table c",
            ],
            SchemaDrift.Compare(expected, actual)
        );
    }



    [Fact]
    public void Compare_when_a_line_has_no_separator_takes_the_whole_line_as_the_item()
    {
        string[] expected = ["table run"];
        string[] actual = ["table run|"];

        Assert.Empty(SchemaDrift.Compare(expected, actual));
    }



    [Fact]
    public void Compare_when_a_line_starts_with_the_separator_takes_the_rest_as_the_detail_of_a_nameless_item()
    {
        string[] expected = ["|first"];
        string[] actual = ["|second"];

        Assert.Equal(["different:  is second, expected first"], SchemaDrift.Compare(expected, actual));
    }



    [Fact]
    public void Compare_when_a_detail_holds_the_separator_keeps_the_rest_as_detail()
    {
        string[] expected = ["index run.ix|not unique (id) where a|b"];
        string[] actual = ["index run.ix|not unique (id) where a|c"];

        Assert.Equal
        (
            ["different: index run.ix is not unique (id) where a|c, expected not unique (id) where a|b"],
            SchemaDrift.Compare(expected, actual)
        );
    }



    [Fact]
    public void Compare_when_expected_is_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SchemaDrift.Compare(null!, []));

        Assert.Equal("expected", exception.ParamName);
    }



    [Fact]
    public void Compare_when_actual_is_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SchemaDrift.Compare([], null!));

        Assert.Equal("actual", exception.ParamName);
    }
}
