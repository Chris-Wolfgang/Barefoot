using Wolfgang.Barefoot.Contracts.Results;
using Wolfgang.Barefoot.Contracts.Runs;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.Results;

public sealed class ResultRecordsTests
{
    private static readonly Guid First = Guid.Parse("5d0c6f0e-3a51-4c1d-8a43-1b2c3d4e5f60");

    private static readonly Guid Second = Guid.Parse("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");

    private static readonly DateTimeOffset Captured = new(2026, 11, 4, 6, 12, 0, TimeSpan.Zero);



    [Theory]
    [InlineData(ResultScope.MatchingFilters, 0)]
    [InlineData(ResultScope.Everything, 1)]
    public void ResultScope_when_cast_to_a_number_keeps_its_declared_value(ResultScope scope, int expected)
    {
        Assert.Equal(expected, (int)scope);
    }



    [Fact]
    public void ResultScope_when_not_set_is_matching_filters()
    {
        Assert.Equal(ResultScope.MatchingFilters, default(ResultScope));
    }



    [Fact]
    public void ResultRunResponse_when_built_keeps_each_value()
    {
        var run = new ResultRunResponse(First, Captured, RunStatus.Incomplete);

        Assert.Equal(First, run.SearchRunId);
        Assert.Equal(Captured, run.CapturedUtc);
        Assert.Equal(RunStatus.Incomplete, run.Outcome);
    }



    [Fact]
    public void ResultSummaryResponse_when_a_run_found_matches_keeps_each_value()
    {
        var cheapest = new CheapestTotalResponse(6142m, "Grande St. Lucian", new DateOnly(2027, 2, 24), "AAA");
        var summary = new ResultSummaryResponse(62, 214, 4918m, cheapest, -486m, Captured, ["Halcyon Beach"]);

        Assert.Equal(62, summary.MatchCount);
        Assert.Equal(214, summary.FoundCount);
        Assert.Equal(4918m, summary.CheapestRoomAmount);
        Assert.Equal(-486m, summary.ChangeAmount);
        Assert.Equal(Captured, summary.PreviousCapturedUtc);
        Assert.Equal(["Halcyon Beach"], summary.OfflineResorts);
        Assert.Equal(6142m, summary.CheapestTotal!.TotalAmount);
        Assert.Equal("Grande St. Lucian", summary.CheapestTotal.ResortName);
        Assert.Equal(new DateOnly(2027, 2, 24), summary.CheapestTotal.CheckIn);
        Assert.Equal("AAA", summary.CheapestTotal.AirportCode);
    }



    [Fact]
    public void ResultSummaryResponse_when_nothing_matched_and_no_run_came_before_leaves_the_prices_empty()
    {
        var summary = new ResultSummaryResponse(0, 12, null, null, null, null, []);

        Assert.Equal(0, summary.MatchCount);
        Assert.Equal(12, summary.FoundCount);
        Assert.Null(summary.CheapestRoomAmount);
        Assert.Null(summary.CheapestTotal);
        Assert.Null(summary.ChangeAmount);
        Assert.Null(summary.PreviousCapturedUtc);
        Assert.Empty(summary.OfflineResorts);
    }



    [Fact]
    public void CheapestTotalResponse_when_the_total_has_no_flights_names_no_airport()
    {
        var cheapest = new CheapestTotalResponse(4918m, "Grande St. Lucian", new DateOnly(2027, 2, 24), null);

        Assert.Null(cheapest.AirportCode);
    }



    [Fact]
    public void ResultRowResponse_when_a_room_has_flights_and_a_sale_keeps_each_value()
    {
        var flights = new ResultFlightsResponse(Second, "AAA", 612m, 1048m, 36);
        var row = new ResultRowResponse
        (
            First,
            Second,
            "Grande St. Lucian",
            new DateOnly(2027, 2, 24),
            7,
            "BCL",
            "Beachfront Club Level Room",
            "Club",
            "Beachfront",
            4918m,
            6410m,
            "$1,500 off",
            [flights],
            5530m,
            -486m,
            IsNew: false,
            MatchesFilters: true
        );

        Assert.Equal(First, row.Id);
        Assert.Equal(Second, row.ResortId);
        Assert.Equal("Grande St. Lucian", row.ResortName);
        Assert.Equal(new DateOnly(2027, 2, 24), row.CheckIn);
        Assert.Equal(7, row.NightCount);
        Assert.Equal("BCL", row.RoomCategoryCode);
        Assert.Equal("Beachfront Club Level Room", row.RoomName);
        Assert.Equal("Club", row.Level);
        Assert.Equal("Beachfront", row.View);
        Assert.Equal(4918m, row.RoomAmount);
        Assert.Equal(6410m, row.RegularAmount);
        Assert.Equal("$1,500 off", row.Promotion);
        Assert.Equal(5530m, row.TotalAmount);
        Assert.Equal(-486m, row.ChangeAmount);
        Assert.False(row.IsNew);
        Assert.True(row.MatchesFilters);

        var airport = Assert.Single(row.Flights);

        Assert.Equal(Second, airport.FlightSetId);
        Assert.Equal("AAA", airport.AirportCode);
        Assert.Equal(612m, airport.CheapestAirfareAmount);
        Assert.Equal(1048m, airport.DearestAirfareAmount);
        Assert.Equal(36, airport.OptionCount);
    }



    [Fact]
    public void ResultRowResponse_when_a_room_is_new_and_has_no_flights_leaves_the_optional_values_empty()
    {
        var row = new ResultRowResponse
        (
            First,
            Second,
            "Grande St. Lucian",
            new DateOnly(2027, 2, 24),
            7,
            "BCL",
            "Beachfront Club Level Room",
            null,
            null,
            4918m,
            null,
            null,
            [],
            4918m,
            null,
            IsNew: true,
            MatchesFilters: false
        );

        Assert.Null(row.Level);
        Assert.Null(row.View);
        Assert.Null(row.RegularAmount);
        Assert.Null(row.Promotion);
        Assert.Empty(row.Flights);
        Assert.Null(row.ChangeAmount);
        Assert.True(row.IsNew);
        Assert.False(row.MatchesFilters);
    }



    [Fact]
    public void FlightOptionResponse_when_built_keeps_each_value_down_to_the_leg()
    {
        var first = new FlightLegResponse
        (
            "AA",
            "1204",
            "American Airlines",
            "Standard Economy",
            new DateOnly(2027, 2, 24),
            new TimeOnly(7, 10),
            "AAA",
            new TimeOnly(10, 5),
            "BBB",
            IsOvernight: false,
            80
        );
        var last = first with
        {
            FlightNumber = "2311",
            DepartureTime = new TimeOnly(11, 25),
            DepartureAirportCode = "BBB",
            ArrivalTime = new TimeOnly(14, 15),
            ArrivalAirportCode = "CCC",
            IsOvernight = true,
            LayoverMinutes = null,
        };
        var outbound = new FlightDirectionResponse([first, last], 1, 365, IsOvernight: true);
        var back = new FlightDirectionResponse([last], 0, null, IsOvernight: false);
        var option = new FlightOptionResponse(First, 612m, outbound, back);

        Assert.Equal(First, option.Id);
        Assert.Equal(612m, option.AirfareAmount);
        Assert.Same(outbound, option.Outbound);
        Assert.Same(back, option.Return);
        Assert.Equal([first, last], outbound.Legs);
        Assert.Equal(1, outbound.StopCount);
        Assert.Equal(365, outbound.DurationMinutes);
        Assert.True(outbound.IsOvernight);
        Assert.Equal(0, back.StopCount);
        Assert.Null(back.DurationMinutes);
        Assert.False(back.IsOvernight);
        Assert.Equal("AA", first.Carrier);
        Assert.Equal("1204", first.FlightNumber);
        Assert.Equal("American Airlines", first.OperatingCarrier);
        Assert.Equal("Standard Economy", first.CabinName);
        Assert.Equal(new DateOnly(2027, 2, 24), first.DepartureDate);
        Assert.Equal(new TimeOnly(7, 10), first.DepartureTime);
        Assert.Equal("AAA", first.DepartureAirportCode);
        Assert.Equal(new TimeOnly(10, 5), first.ArrivalTime);
        Assert.Equal("BBB", first.ArrivalAirportCode);
        Assert.False(first.IsOvernight);
        Assert.Equal(80, first.LayoverMinutes);
        Assert.True(last.IsOvernight);
        Assert.Null(last.LayoverMinutes);
    }
}
