using Wolfgang.Barefoot.Contracts.PriceHistory;

namespace Wolfgang.Barefoot.Contracts.Tests.Unit.PriceHistory;

public sealed class PriceHistoryRecordsTests
{
    private static readonly Guid Search = Guid.Parse("5d0c6f0e-3a51-4c1d-8a43-1b2c3d4e5f60");

    private static readonly Guid Resort = Guid.Parse("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");

    private static readonly Guid Room = Guid.Parse("3f2a1b0c-9d8e-4f7a-b6c5-d4e3f2a1b0c9");

    private static readonly Guid Airport = Guid.Parse("7b0e3c52-1f4a-4d6b-9a57-0c2d8e4f6a11");

    private static readonly DateTimeOffset Captured = new(2026, 11, 4, 6, 12, 0, TimeSpan.Zero);



    [Theory]
    [InlineData(PriceHistoryScope.Mine, 0)]
    [InlineData(PriceHistoryScope.AllUsers, 1)]
    public void PriceHistoryScope_when_cast_to_a_number_keeps_its_declared_value(PriceHistoryScope scope, int expected)
    {
        Assert.Equal(expected, (int)scope);
    }



    [Fact]
    public void PriceHistoryQuery_when_only_the_identity_is_given_asks_for_the_room_alone_over_my_captures()
    {
        var query = new PriceHistoryQuery(Search, Resort, Room, new DateOnly(2027, 2, 24));

        Assert.Equal(Search, query.SearchId);
        Assert.Equal(Resort, query.ResortId);
        Assert.Equal(Room, query.RoomCategoryId);
        Assert.Equal(new DateOnly(2027, 2, 24), query.CheckIn);
        Assert.Null(query.DepartureAirportId);
        Assert.Null(query.From);
        Assert.Equal(PriceHistoryScope.Mine, query.Scope);
    }



    [Fact]
    public void PriceHistoryQuery_when_every_value_is_given_keeps_each_one()
    {
        var query = new PriceHistoryQuery
        (
            Search,
            Resort,
            Room,
            new DateOnly(2027, 2, 24),
            Airport,
            new DateOnly(2026, 8, 6),
            PriceHistoryScope.AllUsers
        );

        Assert.Equal(Airport, query.DepartureAirportId);
        Assert.Equal(new DateOnly(2026, 8, 6), query.From);
        Assert.Equal(PriceHistoryScope.AllUsers, query.Scope);
    }



    [Fact]
    public void PriceHistoryOptionsResponse_when_built_keeps_the_rooms_and_the_dates()
    {
        var room = new PriceHistoryRoomOption(Room, "Beachfront Butler Rondoval");
        var options = new PriceHistoryOptionsResponse([room], [new DateOnly(2027, 2, 23), new DateOnly(2027, 2, 24)]);

        Assert.Equal([room], options.Rooms);
        Assert.Equal([new DateOnly(2027, 2, 23), new DateOnly(2027, 2, 24)], options.CheckIns);
        Assert.Equal(Room, room.RoomCategoryId);
        Assert.Equal("Beachfront Butler Rondoval", room.Name);
    }



    [Fact]
    public void PriceHistoryResponse_when_built_keeps_the_stay_and_its_captures()
    {
        var priced = new PriceCaptureResponse(Captured, 4918m, 1224m, 6142m, -486m);
        var failed = new PriceCaptureResponse(Captured.AddDays(3), null, null, null, null);
        var history = new PriceHistoryResponse(7, 2, 1, 0, [priced, failed]);

        Assert.Equal(7, history.Nights);
        Assert.Equal(2, history.Adults);
        Assert.Equal(1, history.Children);
        Assert.Equal(0, history.Infants);
        Assert.Equal([priced, failed], history.Captures);
        Assert.Equal(Captured, priced.CapturedUtc);
        Assert.Equal(4918m, priced.RoomPrice);
        Assert.Equal(1224m, priced.CheapestFare);
        Assert.Equal(6142m, priced.Total);
        Assert.Equal(-486m, priced.ChangeSincePrevious);
        Assert.Null(failed.RoomPrice);
        Assert.Null(failed.CheapestFare);
        Assert.Null(failed.Total);
        Assert.Null(failed.ChangeSincePrevious);
    }
}
