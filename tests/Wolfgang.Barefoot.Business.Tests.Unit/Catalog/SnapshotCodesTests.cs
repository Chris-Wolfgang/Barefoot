using Wolfgang.Barefoot.Business.Catalog;
using Wolfgang.Barefoot.Contracts.Catalog;
using Wolfgang.Barefoot.Contracts.Searches;

namespace Wolfgang.Barefoot.Business.Tests.Unit.Catalog;

/// <summary>
/// A code repeated within one snapshot never fails the refresh (DESIGN §5; Barefoot#90). The
/// airports and resorts here are made up.
/// </summary>
public sealed class SnapshotCodesTests
{
    private static readonly AirportSnapshotEntry Alpha = new("AAA", "Alpha Field", "Alphaville", "AL", "Exampleland");

    private static readonly AirportSnapshotEntry Bravo = new("BBB", "Bravo Field", "Bravotown", null, "Exampleland");

    private static readonly ResortSnapshotEntry Cove = new("COV", "Sample Cove", Brand.Sandals, "Example Island", "AAA");

    private static readonly ResortSnapshotEntry Dune = new("DUN", "Sample Dune", Brand.Beaches, "Example Island", null);



    [Fact]
    public void Settle_when_every_airport_code_appears_once_keeps_them_all_in_code_order()
    {
        var settled = SnapshotCodes.Settle(new AirportSnapshot([Bravo, Alpha]));

        Assert.Equal([Alpha, Bravo], settled.Entries);
        Assert.Empty(settled.Warnings);
    }



    [Fact]
    public void Settle_when_an_airport_is_repeated_with_the_same_details_keeps_it_once()
    {
        var settled = SnapshotCodes.Settle(new AirportSnapshot([Alpha, Bravo, Alpha with { }]));

        Assert.Equal([Alpha, Bravo], settled.Entries);
        Assert.Empty(settled.Warnings);
    }



    [Fact]
    public void Settle_when_an_airport_is_repeated_with_other_details_leaves_it_out_and_warns()
    {
        var settled = SnapshotCodes.Settle
        (
            new AirportSnapshot([Alpha, Bravo, Alpha with { City = "Elsewhere" }, Alpha])
        );

        Assert.Equal([Bravo], settled.Entries);
        Assert.Equal
        (
            ["The airport AAA appears 3 times with different details. It was left untouched."],
            settled.Warnings
        );
    }



    [Fact]
    public void Settle_when_airport_codes_differ_only_in_case_or_spaces_treats_them_as_one_code()
    {
        var settled = SnapshotCodes.Settle
        (
            new AirportSnapshot([Alpha with { Code = " aaa " }, Alpha, Bravo with { Code = "bbb" }])
        );

        Assert.Equal([Alpha, Bravo], settled.Entries);
        Assert.Empty(settled.Warnings);
    }



    [Fact]
    public void Settle_when_airports_have_no_code_skips_them_and_warns_once()
    {
        var settled = SnapshotCodes.Settle
        (
            new AirportSnapshot([Alpha with { Code = "" }, Bravo, Bravo with { Code = "  ", City = "Elsewhere" }])
        );

        Assert.Equal([Bravo], settled.Entries);
        Assert.Equal(["2 airport entries have no code. They were skipped."], settled.Warnings);
    }



    [Fact]
    public void Settle_when_several_codes_are_left_out_warns_in_code_order()
    {
        var settled = SnapshotCodes.Settle
        (
            new AirportSnapshot
            (
                [
                    Bravo,
                    Alpha,
                    Bravo with { Name = "Other" },
                    Alpha with { Name = "Other" },
                    Alpha with { Code = "" },
                ]
            )
        );

        Assert.Empty(settled.Entries);
        Assert.Equal
        (
            [
                "1 airport entries have no code. They were skipped.",
                "The airport AAA appears 2 times with different details. It was left untouched.",
                "The airport BBB appears 2 times with different details. It was left untouched.",
            ],
            settled.Warnings
        );
    }



    [Fact]
    public void Settle_when_the_airport_snapshot_is_empty_keeps_nothing_and_warns_of_nothing()
    {
        var settled = SnapshotCodes.Settle(new AirportSnapshot([]));

        Assert.Empty(settled.Entries);
        Assert.Empty(settled.Warnings);
    }



    [Fact]
    public void Settle_when_given_no_airport_snapshot_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SnapshotCodes.Settle((AirportSnapshot)null!));

        Assert.Equal("snapshot", exception.ParamName);
    }



    [Fact]
    public void Settle_when_resorts_repeat_keeps_the_agreeing_ones_and_warns_of_the_others()
    {
        var settled = SnapshotCodes.Settle
        (
            new ResortSnapshot
            (
                [
                    Dune,
                    Cove with { Code = "cov" },
                    Cove,
                    Dune with { GatewayAirportCode = "BBB" },
                ]
            )
        );

        Assert.Equal([Cove], settled.Entries);
        Assert.Equal
        (
            ["The resort DUN appears 2 times with different details. It was left untouched."],
            settled.Warnings
        );
    }



    [Fact]
    public void Settle_when_resorts_have_no_code_skips_them_and_warns()
    {
        var settled = SnapshotCodes.Settle(new ResortSnapshot([Cove with { Code = " " }, Dune]));

        Assert.Equal([Dune], settled.Entries);
        Assert.Equal(["1 resort entries have no code. They were skipped."], settled.Warnings);
    }



    [Fact]
    public void Settle_when_given_no_resort_snapshot_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SnapshotCodes.Settle((ResortSnapshot)null!));

        Assert.Equal("snapshot", exception.ParamName);
    }



    [Theory]
    [InlineData("AAA", "AAA")]
    [InlineData(" aaa ", "AAA")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    public void Normalized_when_given_a_code_trims_it_and_makes_it_upper_case(string code, string expected)
    {
        Assert.Equal(expected, SnapshotCodes.Normalized(code));
    }



    [Fact]
    public void Normalized_when_given_null_throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => SnapshotCodes.Normalized(null!));

        Assert.Equal("code", exception.ParamName);
    }
}
